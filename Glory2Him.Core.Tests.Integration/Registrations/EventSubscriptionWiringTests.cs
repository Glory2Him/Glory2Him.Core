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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
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
    /// Proves the event substrate actually comes up, and that a fact published by one service
    /// is received by the handler subscribed to it.
    ///
    /// <para>Nothing else in the suite can prove this. The unit tests mock
    /// <c>IEventBroker</c>, and a mocked broker cannot tell a bound handler from an unbound
    /// one — it will happily report a publish that reached nobody. These tests run the REAL
    /// <c>EventSubscriptionRegistration</c> against a REAL <c>EventBroker</c> on a real
    /// EventHighway store, and assert on <c>EventPublishResult.Deliveries</c>, which carries
    /// one entry per listener that genuinely received the event.</para>
    ///
    /// <para>The fifteen services behind the handlers are mocked, and that costs nothing here:
    /// the subject is the WIRING — which address a fact goes to, and which subscription is
    /// bound to that address — not what a handler does once reached. Whether the workflow then
    /// RESOLVES an approval is a separate question needing real services and a seeded Core
    /// database; see the remarks at the end of this file.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class EventSubscriptionWiringTests
    {
        private readonly EventSubstrateBroker broker;

        public EventSubscriptionWiringTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        [Fact]
        public void ShouldRegisterEverySubscriptionWithoutThrowing()
        {
            // given, when: the fixture ran the real RegisterAsync over all 102 subscriptions

            // then
            this.broker.RegistrationException.Should().BeNull(
                because: "every event address a subscription binds to must be present in its " +
                    "address map — the lookup is a raw indexer, so ONE missing entry throws " +
                    "KeyNotFoundException and aborts every subscription declared after it, " +
                    "leaving the substrate silently half-registered");
        }

        [Fact]
        public async Task ShouldRegisterIdempotentlyAsync()
        {
            // given: the fixture already registered once

            // when
            Func<Task> registeringAgain = async () =>
                await this.broker.Registration.RegisterAsync(CancellationToken.None);

            // then
            await registeringAgain.Should().NotThrowAsync(
                because: "registration is documented as idempotent and safe to call once at " +
                    "startup — every participant, address and listener is written through a " +
                    "RetrieveOrAdd against a stable id, so a restart must not duplicate them");
        }

        [Theory]
        [InlineData(nameof(ContentItem))]
        [InlineData(nameof(Link))]
        public async Task ShouldRouteTheVersionedEntityFromTheProcessingTierOnlyAsync(
            string entityName)
        {
            // given: a versioned entity publishes an -Added fact from BOTH tiers, on two
            // different addresses. The workflow is routed from PROCESSING (design §12.4.1
            // rules 6-7), because that tier owns fork-on-modify.
            //
            // Both halves are asserted together on purpose. A bare "did not reach" assertion
            // passes just as well when the substrate is dead and NOTHING is delivered, so it
            // proves nothing on its own; pairing it with the positive half means this can only
            // pass when delivery genuinely works AND the split is genuinely correct.
            Guid workflowSubscriptionId = ProcessingAddedSubscriptionId(entityName);

            // when
            IReadOnlyList<Guid> reachedFromProcessing =
                await PublishProcessingAddedFactAsync(entityName);

            IReadOnlyList<Guid> reachedFromFoundation =
                await PublishFoundationAddedFactAsync(entityName);

            // then
            reachedFromProcessing.Should().Contain(
                workflowSubscriptionId,
                because: $"the approval workflow must hear about a {entityName} add, and for a " +
                    "versioned entity the processing tier is the fact it is meant to hear");

            reachedFromFoundation.Should().NotContain(
                workflowSubscriptionId,
                because: $"a versioned entity's foundation {entityName}-Added must not ALSO " +
                    "reach the approval workflow — hearing both would evaluate the same " +
                    "approval twice for one edit");
        }

        [Theory]
        [InlineData(nameof(Tag))]
        [InlineData(nameof(Comment))]
        [InlineData(nameof(Reaction))]
        [InlineData(nameof(BibleReference))]
        [InlineData(nameof(Association))]
        public async Task ShouldReachTheApprovalWorkflowFromTheFoundationTierAsync(
            string entityName)
        {
            // given: the five single-row entities have no processing tier, so the workflow is
            // routed from their foundation fact (design §12.4.1 rules 6-7)

            // when
            IReadOnlyList<Guid> subscriptionsReached =
                await PublishFoundationAddedFactAsync(entityName);

            // then
            subscriptionsReached.Should().Contain(
                FoundationAddedSubscriptionId(entityName),
                because: $"{entityName} is a single-row entity with no processing tier, so its " +
                    "foundation -Added fact is the only thing that can open its approval");
        }

        [Fact]
        public async Task ShouldReachTheApprovalWorkflowWhenAReviewIsRecordedAsync()
        {
            // given: a recorded review moves the §8.5 approval count

            // when
            EventPublishResult<ApprovalReview> publishResult =
                await this.broker.EventBroker.PublishApprovalReviewAsync(
                    new EventEnvelope<ApprovalReview>
                    {
                        Content = new ApprovalReview { Id = Guid.NewGuid() }
                    },
                    ApprovalReviewEventOperation.Added);

            // then
            EventSubstrateBroker.SubscriptionsReached(publishResult).Should().Contain(
                EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId,
                because: "an added review either moves the approval count or raises a blocking " +
                    "rejection, so it must re-test the approval");
        }

        [Fact]
        public async Task ShouldNotDeliverAnEntitysFactToAnotherEntitysSubscriptionAsync()
        {
            // given: each entity's fact address is its own. A subscription bound to the wrong
            // one is invisible to every unit test, because a mocked broker routes nothing

            Guid[] otherEntitiesWorkflowSubscriptions = new[]
            {
                EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionId,
                EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionId,
                EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionId,
                EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionId,
                EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId,
                EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionId
            };

            // when: a Tag fact is published
            IReadOnlyList<Guid> subscriptionsReached =
                await PublishFoundationAddedFactAsync(nameof(Tag));

            // then: the positive half first, so a dead substrate cannot satisfy this test by
            // delivering nothing at all
            subscriptionsReached.Should().Contain(
                EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId,
                because: "the Tag-Added fact must reach Tag's own workflow subscription");

            subscriptionsReached.Should().NotIntersectWith(
                otherEntitiesWorkflowSubscriptions,
                because: "and no other entity's — a fact address shared between two entities " +
                    "would fire the approval workflow for the wrong row");
        }

        private async Task<IReadOnlyList<Guid>> PublishFoundationAddedFactAsync(
            string entityName) =>
                entityName switch
                {
                    nameof(ContentItem) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishContentItemAsync(
                            new EventEnvelope<ContentItem> { Content = new ContentItem() },
                            ContentItemEventOperation.Added)),

                    nameof(Link) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishLinkAsync(
                            new EventEnvelope<Link> { Content = new Link() },
                            LinkEventOperation.Added)),

                    nameof(Tag) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishTagAsync(
                            new EventEnvelope<Tag> { Content = new Tag() },
                            TagEventOperation.Added)),

                    nameof(Comment) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishCommentAsync(
                            new EventEnvelope<Comment> { Content = new Comment() },
                            CommentEventOperation.Added)),

                    nameof(Reaction) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishReactionAsync(
                            new EventEnvelope<Reaction> { Content = new Reaction() },
                            ReactionEventOperation.Added)),

                    nameof(BibleReference) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishBibleReferenceAsync(
                            new EventEnvelope<BibleReference> { Content = new BibleReference() },
                            BibleReferenceEventOperation.Added)),

                    nameof(Association) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishAssociationAsync(
                            new EventEnvelope<Association> { Content = new Association() },
                            AssociationEventOperation.Added)),

                    _ => throw new ArgumentOutOfRangeException(nameof(entityName), entityName,
                        "No foundation -Added publish is mapped for this entity.")
                };

        private async Task<IReadOnlyList<Guid>> PublishProcessingAddedFactAsync(
            string entityName) =>
                entityName switch
                {
                    nameof(ContentItem) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishContentItemProcessingAsync(
                            new EventEnvelope<ContentItem> { Content = new ContentItem() },
                            ContentItemProcessingEventOperation.Added)),

                    nameof(Link) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishLinkProcessingAsync(
                            new EventEnvelope<Link> { Content = new Link() },
                            LinkProcessingEventOperation.Added)),

                    _ => throw new ArgumentOutOfRangeException(nameof(entityName), entityName,
                        "Only the versioned entities have a processing tier.")
                };

        private static Guid ProcessingAddedSubscriptionId(string entityName) =>
            entityName switch
            {
                nameof(ContentItem) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionId,

                nameof(Link) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionId,

                _ => throw new ArgumentOutOfRangeException(nameof(entityName), entityName,
                    "Only the versioned entities have a processing tier.")
            };

        private static Guid FoundationAddedSubscriptionId(string entityName) =>
            entityName switch
            {
                nameof(Tag) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId,

                nameof(Comment) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionId,

                nameof(Reaction) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionId,

                nameof(BibleReference) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId,

                nameof(Association) =>
                    EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionId,

                _ => throw new ArgumentOutOfRangeException(nameof(entityName), entityName,
                    "No foundation-tier workflow subscription is mapped for this entity.")
            };
    }
}
