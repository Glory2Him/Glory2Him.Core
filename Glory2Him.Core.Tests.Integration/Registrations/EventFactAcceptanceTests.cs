// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// Proves a fact is not merely DELIVERED to the approval workflow but ACCEPTED by it.
    ///
    /// <para>The sibling wiring tests answer "which subscription received this?" and stop there,
    /// because they run against a mocked orchestration. That leaves a whole defect class
    /// invisible: the receiver re-verifies the envelope's HMAC against the event name it
    /// expects, and the event name is bound INTO the signature. So a publisher that signs one
    /// name and a receiver that verifies another produce a delivery that arrives and is then
    /// refused by its own recipient — silently, since nothing else in the system watches.</para>
    ///
    /// <para>These tests use the real <c>ApprovalOrchestrationService</c> with the same
    /// integrity broker the publisher signs with. Same key, same algorithm — so the only thing
    /// that can differ is the NAME, and the only thing that can break the payload is the wire.
    /// A delivery that reports success is a fact the receiver ran to completion on.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class EventFactAcceptanceTests
    {
        private readonly EventSubstrateBroker broker;

        public EventFactAcceptanceTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        // -Modified only. -Added delivery for these two is already pinned exactly by
        // EventSubscriptionWiringTests.ShouldRouteTheVersionedEntityFromTheProcessingTierOnlyAsync,
        // so re-proving it here would be the same mechanism proven twice (#487). -Modified is
        // kept because nothing else in the suite ever publishes it through the real substrate —
        // §9.7.4 re-approval-on-change depends on it, and a refused -Modified means an
        // already-Approved row that is then edited silently keeps its stale verdict.
        [Theory]
        [InlineData(nameof(ContentItem))]
        [InlineData(nameof(Link))]
        public async Task ShouldAcceptTheVersionedEntityModifiedFactFromItsProcessingTierAsync(
            string entityName)
        {
            // given: the processing tier is the tier that owns these two entities' top-layer
            // fact, so the name it signs is the name its receiver must verify

            // when
            IReadOnlyList<bool> outcomes = entityName switch
            {
                nameof(ContentItem) => DeliveryOutcomes(
                    await this.broker.EventBroker.PublishContentItemProcessingAsync(
                        new EventEnvelope<ContentItem>
                        {
                            Content = new ContentItem { Id = Guid.NewGuid() }
                        },
                        ContentItemProcessingEventOperation.Modified)),

                nameof(Link) => DeliveryOutcomes(
                    await this.broker.EventBroker.PublishLinkProcessingAsync(
                        new EventEnvelope<Link> { Content = new Link { Id = Guid.NewGuid() } },
                        LinkProcessingEventOperation.Modified)),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(entityName), entityName, "Only the versioned entities have a tier.")
            };

            // then
            outcomes.Should().Equal(new[] { true },
                because: $"the approval workflow must ACCEPT the {entityName} Modified fact " +
                    "its own processing tier signed. The event name is inside the HMAC, so a " +
                    "receiver verifying a different name than the publisher composed refuses a " +
                    "genuine envelope — the fact arrives and is thrown away by its own recipient");
        }

        // -Modified only — see the note above. -Added delivery for these four is already pinned
        // exactly by EventSubscriptionWiringTests.ShouldReachTheApprovalWorkflowFromTheFoundationTierAsync.
        [Theory]
        [InlineData(nameof(Tag))]
        [InlineData(nameof(Comment))]
        [InlineData(nameof(Reaction))]
        [InlineData(nameof(BibleReference))]
        public async Task ShouldAcceptTheSingleRowEntityModifiedFactFromItsFoundationAsync(
            string entityName)
        {
            // given: these four have no processing tier, so the foundation signs their fact

            // when
            IReadOnlyList<bool> outcomes =
                await PublishFoundationFactAsync(entityName, isModifiedFact: true);

            // then
            outcomes.Should().Equal(new[] { true },
                because: $"the approval workflow must ACCEPT the {entityName} Modified fact " +
                    "its own foundation signed");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShouldAcceptAnAssociationFactCarryingItsEffectiveIdsAsync(
            bool isModifiedFact)
        {
            // given: EntityAEffectiveId and EntityBEffectiveId are computed and persisted by the
            // database, so a real published Association fact always carries them. They are set
            // here by reflection because they are `private set` — exactly as EF materialises
            // them, and exactly the property shape that does not survive System.Text.Json.
            var association = new Association { Id = Guid.NewGuid() };

            typeof(Association).GetProperty(nameof(Association.EntityAEffectiveId))
                .SetValue(association, Guid.NewGuid());

            typeof(Association).GetProperty(nameof(Association.EntityBEffectiveId))
                .SetValue(association, Guid.NewGuid());

            // when
            IReadOnlyList<bool> outcomes = DeliveryOutcomes(
                await this.broker.EventBroker.PublishAssociationAsync(
                    new EventEnvelope<Association> { Content = association },
                    isModifiedFact
                        ? AssociationEventOperation.Modified
                        : AssociationEventOperation.Added));

            // then
            outcomes.Should().Equal(new[] { true },
                because: "the signature is computed over the association as published and " +
                    "re-computed over the association as received, so any property lost " +
                    "between the two breaks it — a value the publisher signed and the receiver " +
                    "cannot see makes the receiver refuse a genuine envelope");
        }

        // -Submitted, for all seven. Every one of these fires on the FOUNDATION's bare name even
        // for ContentItem and Link, whose Added/Modified facts come from the processing tier
        // above — the submit verb is a foundation transition on every approvable entity, and
        // nothing above the foundation takes part in it
        // (ApprovalOrchestrationService.Substrate.cs:205-210 states this explicitly: a
        // "ContentItemProcessingSubmitted" name would verify nothing, ever). This is the pairing
        // #487 found proven nowhere: `ApprovalOrchestrationServiceTests.Substrate.cs` already
        // proves the RECEIVER's literal is self-consistent for all 21 entity-fact handlers, but
        // nothing published a real -Submitted fact through the real substrate until this theory —
        // so a publisher/receiver name mismatch specific to Submitted had no way to surface.
        [Theory]
        [InlineData(nameof(Tag))]
        [InlineData(nameof(ContentItem))]
        [InlineData(nameof(Link))]
        [InlineData(nameof(Comment))]
        [InlineData(nameof(Reaction))]
        [InlineData(nameof(BibleReference))]
        [InlineData(nameof(Association))]
        public async Task ShouldAcceptTheSubmittedFactFromItsFoundationAsync(string entityName)
        {
            // given: the submit verb reaches the foundation directly regardless of which tier
            // owns the Added/Modified fact, so every entity signs its own bare name here

            // when
            IReadOnlyList<bool> outcomes = await PublishFoundationSubmittedFactAsync(entityName);

            // then
            outcomes.Should().Equal(new[] { true },
                because: $"the approval workflow must ACCEPT the {entityName} Submitted fact " +
                    "its own foundation signed — this is the arm the receiver's literal was " +
                    "never checked against a real publish for");
        }

        private async Task<IReadOnlyList<bool>> PublishFoundationSubmittedFactAsync(
            string entityName) =>
                entityName switch
                {
                    nameof(Tag) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishTagAsync(
                            new EventEnvelope<Tag> { Content = new Tag { Id = Guid.NewGuid() } },
                            TagEventOperation.Submitted)),

                    nameof(ContentItem) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishContentItemAsync(
                            new EventEnvelope<ContentItem>
                            {
                                Content = new ContentItem { Id = Guid.NewGuid() }
                            },
                            ContentItemEventOperation.Submitted)),

                    nameof(Link) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishLinkAsync(
                            new EventEnvelope<Link> { Content = new Link { Id = Guid.NewGuid() } },
                            LinkEventOperation.Submitted)),

                    nameof(Comment) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishCommentAsync(
                            new EventEnvelope<Comment>
                            {
                                Content = new Comment { Id = Guid.NewGuid() }
                            },
                            CommentEventOperation.Submitted)),

                    nameof(Reaction) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishReactionAsync(
                            new EventEnvelope<Reaction>
                            {
                                Content = new Reaction { Id = Guid.NewGuid() }
                            },
                            ReactionEventOperation.Submitted)),

                    nameof(BibleReference) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishBibleReferenceAsync(
                            new EventEnvelope<BibleReference>
                            {
                                Content = new BibleReference { Id = Guid.NewGuid() }
                            },
                            BibleReferenceEventOperation.Submitted)),

                    nameof(Association) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishAssociationAsync(
                            new EventEnvelope<Association>
                            {
                                Content = new Association { Id = Guid.NewGuid() }
                            },
                            AssociationEventOperation.Submitted)),

                    _ => throw new ArgumentOutOfRangeException(
                        nameof(entityName), entityName,
                        "No foundation Submitted publish is mapped for this entity.")
                };

        private async Task<IReadOnlyList<bool>> PublishFoundationFactAsync(
            string entityName,
            bool isModifiedFact) =>
                entityName switch
                {
                    nameof(Tag) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishTagAsync(
                            new EventEnvelope<Tag> { Content = new Tag { Id = Guid.NewGuid() } },
                            isModifiedFact
                                ? TagEventOperation.Modified
                                : TagEventOperation.Added)),

                    nameof(Comment) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishCommentAsync(
                            new EventEnvelope<Comment>
                            {
                                Content = new Comment { Id = Guid.NewGuid() }
                            },
                            isModifiedFact
                                ? CommentEventOperation.Modified
                                : CommentEventOperation.Added)),

                    nameof(Reaction) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishReactionAsync(
                            new EventEnvelope<Reaction>
                            {
                                Content = new Reaction { Id = Guid.NewGuid() }
                            },
                            isModifiedFact
                                ? ReactionEventOperation.Modified
                                : ReactionEventOperation.Added)),

                    nameof(BibleReference) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishBibleReferenceAsync(
                            new EventEnvelope<BibleReference>
                            {
                                Content = new BibleReference { Id = Guid.NewGuid() }
                            },
                            isModifiedFact
                                ? BibleReferenceEventOperation.Modified
                                : BibleReferenceEventOperation.Added)),

                    _ => throw new ArgumentOutOfRangeException(
                        nameof(entityName), entityName,
                        "No foundation fact publish is mapped for this entity.")
                };

        // The per-listener success flags, in order. Asserted as a whole sequence rather than
        // "any succeeded", so a fact reaching nobody reads as an empty sequence and fails
        // rather than passing for want of a counter-example.
        private static IReadOnlyList<bool> DeliveryOutcomes<T>(
            EventPublishResult<T> publishResult) =>
                (publishResult.Deliveries ?? new List<EventDelivery<T>>())
                    .Select(delivery => delivery.IsSuccess)
                    .ToList();
    }
}
