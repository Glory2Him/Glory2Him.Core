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

        [Theory]
        [InlineData(nameof(ContentItem))]
        [InlineData(nameof(Link))]
        public async Task ShouldAcceptTheVersionedEntityFactFromItsProcessingTierAsync(
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
                        ContentItemProcessingEventOperation.Added)),

                nameof(Link) => DeliveryOutcomes(
                    await this.broker.EventBroker.PublishLinkProcessingAsync(
                        new EventEnvelope<Link> { Content = new Link { Id = Guid.NewGuid() } },
                        LinkProcessingEventOperation.Added)),

                _ => throw new ArgumentOutOfRangeException(nameof(entityName))
            };

            // then
            outcomes.Should().Equal(new[] { true },
                because: $"the approval workflow must ACCEPT the {entityName} fact its own " +
                    "processing tier signed. The event name is inside the HMAC, so a receiver " +
                    "verifying a different name than the publisher composed refuses a genuine " +
                    "envelope — the fact arrives and is thrown away by its own recipient");
        }

        [Theory]
        [InlineData(nameof(Tag))]
        [InlineData(nameof(Comment))]
        [InlineData(nameof(Reaction))]
        [InlineData(nameof(BibleReference))]
        public async Task ShouldAcceptTheSingleRowEntityFactFromItsFoundationAsync(
            string entityName)
        {
            // given: these four have no processing tier, so the foundation signs their fact

            // when
            IReadOnlyList<bool> outcomes = await PublishFoundationAddedFactAsync(entityName);

            // then
            outcomes.Should().Equal(new[] { true },
                because: $"the approval workflow must ACCEPT the {entityName} fact its own " +
                    "foundation signed");
        }

        [Fact]
        public async Task ShouldAcceptAnAssociationFactCarryingItsEffectiveIdsAsync()
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
                    AssociationEventOperation.Added));

            // then
            outcomes.Should().Equal(new[] { true },
                because: "the signature is computed over the association as published and " +
                    "re-computed over the association as received, so any property lost " +
                    "between the two breaks it — a value the publisher signed and the receiver " +
                    "cannot see makes the receiver refuse a genuine envelope");
        }

        private async Task<IReadOnlyList<bool>> PublishFoundationAddedFactAsync(
            string entityName) =>
                entityName switch
                {
                    nameof(Tag) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishTagAsync(
                            new EventEnvelope<Tag> { Content = new Tag { Id = Guid.NewGuid() } },
                            TagEventOperation.Added)),

                    nameof(Comment) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishCommentAsync(
                            new EventEnvelope<Comment>
                            {
                                Content = new Comment { Id = Guid.NewGuid() }
                            },
                            CommentEventOperation.Added)),

                    nameof(Reaction) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishReactionAsync(
                            new EventEnvelope<Reaction>
                            {
                                Content = new Reaction { Id = Guid.NewGuid() }
                            },
                            ReactionEventOperation.Added)),

                    nameof(BibleReference) => DeliveryOutcomes(
                        await this.broker.EventBroker.PublishBibleReferenceAsync(
                            new EventEnvelope<BibleReference>
                            {
                                Content = new BibleReference { Id = Guid.NewGuid() }
                            },
                            BibleReferenceEventOperation.Added)),

                    _ => throw new ArgumentOutOfRangeException(nameof(entityName))
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
