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
    /// <para>The subject here is the WIRING — which address a fact goes to, and which
    /// subscription is bound to that address — not what a handler does once reached. These
    /// assertions therefore read <c>SubscriptionId</c> and never <c>IsSuccess</c>: a fact is
    /// routed to a listener whether or not the handler behind it then succeeds, so routing is
    /// observable independently of the outcome.
    ///
    /// Whether a delivered fact is ACCEPTED by its receiver is the separate question
    /// <c>EventFactAcceptanceTests</c> answers, against the same fixture.</para>
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
            // given, when: the fixture ran the real RegisterAsync over all 109 subscriptions

            // then
            this.broker.RegistrationException.Should().BeNull(
                because: "every event address a subscription binds to must be present in its " +
                    "address map — the lookup is a raw indexer, so ONE missing entry throws " +
                    "KeyNotFoundException and aborts every subscription declared after it, " +
                    "leaving the substrate silently half-registered");
        }

        [Fact]
        public void ShouldRegisterIdempotently()
        {
            // given, when: the fixture ran RegisterAsync a second time while building.
            //
            // Registering from here instead would mean one test mutating the substrate every
            // other test in this collection shares, at a point in the order xUnit does not
            // guarantee. Doing it during construction also makes every delivery assertion below
            // run against a doubly registered substrate, so a duplicated listener would surface
            // as a delivery count of two rather than passing unnoticed.

            // then
            this.broker.SecondRegistrationException.Should().BeNull(
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
            EventSubstrateBroker.WorkflowSubscriptionsReached(reachedFromProcessing)
                .Should().Equal(new[] { workflowSubscriptionId },
                    because: $"the approval workflow must hear about a {entityName} add exactly " +
                        "once, through that one subscription and no other — a second binding on " +
                        "this address, whatever its id, evaluates the approval twice per edit");

            EventSubstrateBroker.WorkflowSubscriptionsReached(reachedFromFoundation)
                .Should().BeEmpty(
                    because: $"a versioned entity's foundation {entityName}-Added must not reach " +
                        "the approval workflow at all — it is routed from the processing tier, " +
                        "and hearing both would evaluate the same approval twice for one edit");
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
            EventSubstrateBroker.WorkflowSubscriptionsReached(subscriptionsReached)
                .Should().Equal(new[] { FoundationAddedSubscriptionId(entityName) },
                    because: $"{entityName} is a single-row entity with no processing tier, so " +
                        "its foundation -Added fact is the only thing that can open its " +
                        "approval — exactly once, and through no other workflow subscription");
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
            EventSubstrateBroker.WorkflowSubscriptionsReached(
                EventSubstrateBroker.SubscriptionsReached(publishResult))
                .Should().Equal(
                    new[]
                    {
                        EventBrokerIdentifiers
                            .ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId
                    },
                    because: "an added review either moves the approval count or raises a " +
                        "blocking rejection, so it must re-test the approval exactly once");
        }

        [Fact]
        public async Task ShouldNotDeliverAnEntitysFactToAnotherEntitysSubscriptionAsync()
        {
            // given: each entity's fact address is its own. A subscription bound to the wrong
            // one is invisible to every unit test, because a mocked broker routes nothing.
            //
            // This asserts the ENTIRE delivery set rather than the workflow subset the other
            // tests use, which is what makes it worth keeping alongside them: it fails on ANY
            // unexpected listener, workflow-owned or not, so a foreign subscription that quietly
            // shares this address has nowhere to hide.

            // when: a Tag fact is published
            IReadOnlyList<Guid> subscriptionsReached =
                await PublishFoundationAddedFactAsync(nameof(Tag));

            // then
            subscriptionsReached.Should().Equal(
                new[] { EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId },
                because: "Tag-Added is Tag's own address: it must reach Tag's workflow " +
                    "subscription and nothing else — an address shared with another entity " +
                    "would fire the approval workflow for the wrong row");
        }

        private async Task<IReadOnlyList<Guid>> PublishFoundationAddedFactAsync(
            string entityName) =>
                entityName switch
                {
                    nameof(ContentItem) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishContentItemAsync(
                            new EventEnvelope<ContentItem> { Content = new ContentItem { Id = Guid.NewGuid() } },
                            ContentItemEventOperation.Added)),

                    nameof(Link) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishLinkAsync(
                            new EventEnvelope<Link> { Content = new Link { Id = Guid.NewGuid() } },
                            LinkEventOperation.Added)),

                    nameof(Tag) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishTagAsync(
                            new EventEnvelope<Tag> { Content = new Tag { Id = Guid.NewGuid() } },
                            TagEventOperation.Added)),

                    nameof(Comment) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishCommentAsync(
                            new EventEnvelope<Comment> { Content = new Comment { Id = Guid.NewGuid() } },
                            CommentEventOperation.Added)),

                    nameof(Reaction) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishReactionAsync(
                            new EventEnvelope<Reaction> { Content = new Reaction { Id = Guid.NewGuid() } },
                            ReactionEventOperation.Added)),

                    nameof(BibleReference) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishBibleReferenceAsync(
                            new EventEnvelope<BibleReference> { Content = new BibleReference { Id = Guid.NewGuid() } },
                            BibleReferenceEventOperation.Added)),

                    nameof(Association) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishAssociationAsync(
                            new EventEnvelope<Association> { Content = new Association { Id = Guid.NewGuid() } },
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
                            new EventEnvelope<ContentItem> { Content = new ContentItem { Id = Guid.NewGuid() } },
                            ContentItemProcessingEventOperation.Added)),

                    nameof(Link) => EventSubstrateBroker.SubscriptionsReached(
                        await this.broker.EventBroker.PublishLinkProcessingAsync(
                            new EventEnvelope<Link> { Content = new Link { Id = Guid.NewGuid() } },
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
