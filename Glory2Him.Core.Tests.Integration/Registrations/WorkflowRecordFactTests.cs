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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// Proves every fact address on the two workflow records reaches the §8.5 re-test
    /// (design §10.17(a)).
    ///
    /// <para>The contract is that EVERY one of the eight has a subscriber, not a useful subset,
    /// because each can move a predicate the evaluation reads: comments through
    /// <c>IsDeleted is false &amp;&amp; IsResolved is false</c>, reviews through
    /// <c>IsDeleted is false &amp;&amp; Verdict != Dismissed</c>. An address left unwatched is a
    /// gate that moves unnoticed — an approval blocked only by an outstanding comment stays
    /// blocked after the comment is resolved.</para>
    ///
    /// <para>Two of the eight are shared: <c>HardRemoved</c> is published to the <c>Removed</c>
    /// address on purpose, so those handlers receive envelopes signed under either name. Since
    /// the event name is inside the HMAC, a handler expecting only one of them refuses half its
    /// traffic silently — which is why the hard-removal rows below are not redundant with the
    /// soft ones.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class WorkflowRecordFactTests
    {
        private readonly EventSubstrateBroker broker;

        public WorkflowRecordFactTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        public static TheoryData<string> ReviewFactOperations() =>
            new TheoryData<string>
            {
                nameof(ApprovalReviewEventOperation.Added),
                nameof(ApprovalReviewEventOperation.Modified),
                nameof(ApprovalReviewEventOperation.Removed),
                nameof(ApprovalReviewEventOperation.HardRemoved),
                nameof(ApprovalReviewEventOperation.Dismissed)
            };

        public static TheoryData<string> CommentFactOperations() =>
            new TheoryData<string>
            {
                nameof(ApprovalCommentEventOperation.Added),
                nameof(ApprovalCommentEventOperation.Modified),
                nameof(ApprovalCommentEventOperation.Resolved),
                nameof(ApprovalCommentEventOperation.Removed),
                nameof(ApprovalCommentEventOperation.HardRemoved)
            };

        [Theory]
        [MemberData(nameof(ReviewFactOperations))]
        public async Task ShouldReTestTheRoundAnApprovalReviewFactBelongsToAsync(
            string operationName)
        {
            // given: the round the review names, which is what the handler must key on — the
            // review's own Id is deliberately different, so a handler reaching for the wrong
            // one is visible rather than merely still succeeding
            Guid approvalId = Guid.NewGuid();
            int readsBefore = this.broker.ApprovalIdsRead.Count;

            var operation = Enum.Parse<ApprovalReviewEventOperation>(operationName);

            // when
            EventPublishResult<ApprovalReview> publishResult =
                await this.broker.EventBroker.PublishApprovalReviewAsync(
                    new EventEnvelope<ApprovalReview>
                    {
                        Content = new ApprovalReview
                        {
                            Id = Guid.NewGuid(),
                            ApprovalId = approvalId
                        }
                    },
                    operation);

            // then
            DeliveryOutcomes(publishResult).Should().Equal(new[] { true },
                because: $"ApprovalReview-{operationName} moves a §8.5 predicate, so it must " +
                    "reach the workflow AND be accepted by it — a removal fact signed " +
                    "'HardRemoved' arrives on the same address as 'Removed', and the event " +
                    "name is inside the HMAC");

            this.broker.ApprovalIdsRead.Skip(readsBefore).Should().Equal(new[] { approvalId },
                because: "the handler must re-test the ROUND the review belongs to, keyed on " +
                    "ApprovalId — a handler keyed on the review's own Id still delivers " +
                    "successfully, so only the id it read distinguishes the two");
        }

        [Theory]
        [MemberData(nameof(CommentFactOperations))]
        public async Task ShouldReTestTheRoundAnApprovalCommentFactBelongsToAsync(
            string operationName)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            int readsBefore = this.broker.ApprovalIdsRead.Count;

            var operation = Enum.Parse<ApprovalCommentEventOperation>(operationName);

            // when
            EventPublishResult<ApprovalComment> publishResult =
                await this.broker.EventBroker.PublishApprovalCommentAsync(
                    new EventEnvelope<ApprovalComment>
                    {
                        Content = new ApprovalComment
                        {
                            Id = Guid.NewGuid(),
                            ApprovalId = approvalId,

                            // Born SETTLED, which §7.8 makes the common case. It moves nothing —
                            // and the point is that the handler establishes that by evaluating
                            // rather than by reading the flag and deciding for itself.
                            IsResolved = true
                        }
                    },
                    operation);

            // then
            DeliveryOutcomes(publishResult).Should().Equal(new[] { true },
                because: $"ApprovalComment-{operationName} moves a §8.5 predicate, so it must " +
                    "reach the workflow and be accepted by it");

            this.broker.ApprovalIdsRead.Skip(readsBefore).Should().Equal(new[] { approvalId },
                because: "a comment born settled still re-tests the round — the evaluation is " +
                    "what establishes that nothing moved, never an inference from the payload " +
                    "or from which address the fact arrived on");
        }

        [Fact]
        public async Task ShouldRouteEveryWorkflowRecordFactToItsOwnSubscriptionAsync()
        {
            // given: eight addresses, each its own subscription. A fact landing on another
            // record's subscription would re-test a round it has nothing to do with

            // when
            IReadOnlyList<Guid> reachedByReviewDismissal =
                EventSubstrateBroker.SubscriptionsReached(
                    await this.broker.EventBroker.PublishApprovalReviewAsync(
                        new EventEnvelope<ApprovalReview>
                        {
                            Content = new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = Guid.NewGuid()
                            }
                        },
                        ApprovalReviewEventOperation.Dismissed));

            // then
            EventSubstrateBroker.WorkflowSubscriptionsReached(reachedByReviewDismissal)
                .Should().Equal(
                    new[]
                    {
                        EventBrokerIdentifiers
                            .ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionId
                    },
                    because: "a dismissal reaches the dismissal subscription and no other — " +
                        "the eight addresses are distinct and a fact must not fan out across " +
                        "them");
        }

        [Fact]
        public void ShouldGiveEveryWorkflowRecordFactAddressASubscriber()
        {
            // given: §10.17(a) is a contract over the ADDRESSES, not over a list somebody types.
            // The theories above cover the ten past-tense operations that exist today; this
            // fails if an eleventh is added tomorrow and nobody wires it.
            //
            // Counted by distinct ADDRESS rather than by operation, because HardRemoved is
            // published to the Removed address — five operations, four addresses, per record.
            IReadOnlyList<Guid> reviewFactAddresses = DistinctFactAddresses(
                EventBrokerIdentifiers.ApprovalReviewEventAddressIds);

            IReadOnlyList<Guid> commentFactAddresses = DistinctFactAddresses(
                EventBrokerIdentifiers.ApprovalCommentEventAddressIds);

            // when
            int workflowRecordSubscriptions = EventSubstrateBroker.WorkflowSubscriptionIds
                .Count(subscriptionId => WorkflowRecordSubscriptionIds.Contains(subscriptionId));

            // then
            (reviewFactAddresses.Count + commentFactAddresses.Count).Should().Be(
                workflowRecordSubscriptions,
                because: "every fact address on both workflow records must have a subscriber " +
                    "(§10.17(a)) — each one can move a §8.5 predicate, so an address left " +
                    "unwatched is a gate that moves unnoticed. A past-tense operation added to " +
                    "either enum without a subscription fails here rather than in production");
        }

        private static readonly HashSet<Guid> WorkflowRecordSubscriptionIds = new()
        {
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewRemovedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalCommentAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalCommentModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalCommentResolvedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalCommentRemovedSubscriptionId
        };

        // A fact is the past-tense half of the enum. Requests end in -ing and are answered by
        // the record's own service, not by the workflow.
        private static IReadOnlyList<Guid> DistinctFactAddresses<TOperation>(
            IReadOnlyDictionary<TOperation, Guid> eventAddressIds)
            where TOperation : struct, Enum =>
                eventAddressIds
                    .Where(entry => entry.Key.ToString().EndsWith("ed", StringComparison.Ordinal))
                    .Where(entry => entry.Key.ToString().EndsWith("ing", StringComparison.Ordinal)
                        is false)
                    .Select(entry => entry.Value)
                    .Distinct()
                    .ToList();

        private static IReadOnlyList<bool> DeliveryOutcomes<T>(
            EventPublishResult<T> publishResult) =>
                (publishResult.Deliveries ?? new List<EventDelivery<T>>())
                    .Select(delivery => delivery.IsSuccess)
                    .ToList();
    }
}
