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

        /// <summary>
        /// Every FACT operation on the record, derived from the enum rather than listed.
        /// </summary>
        /// <remarks>
        /// Derived on purpose. §10.17(a) is a contract over every address, and a hand-typed
        /// list only ever covers the operations somebody remembered — the failure mode it
        /// cannot see is precisely the one that matters, an operation added later and wired
        /// nowhere. Deriving means a new fact operation arrives with a test row already
        /// attached, and that row fails until it is both subscribed and given an accepted name.
        ///
        /// <para>Facts are identified by EXCLUDING requests, never by matching a past-tense
        /// suffix. A suffix test looks equivalent and is not: <c>ConfidenceSet</c> already
        /// ships on <c>AssociationEventOperation</c> as a fact that ends in neither "ed" nor
        /// "ing", so an include-list keyed on "ed" would silently classify a new fact of that
        /// shape as a request and stop testing it.</para>
        /// </remarks>
        private static IReadOnlyList<string> FactOperationNamesOf<TOperation>()
            where TOperation : struct, Enum =>
                Enum.GetNames<TOperation>()
                    .Where(name => IsRequestName(name) is false)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();

        private static TheoryData<string> FactOperationsOf<TOperation>()
            where TOperation : struct, Enum
        {
            var operations = new TheoryData<string>();

            foreach (string factName in FactOperationNamesOf<TOperation>())
            {
                operations.Add(factName);
            }

            return operations;
        }

        public static TheoryData<string> ReviewFactOperations() =>
            FactOperationsOf<ApprovalReviewEventOperation>();

        public static TheoryData<string> CommentFactOperations() =>
            FactOperationsOf<ApprovalCommentEventOperation>();

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
        public void ShouldCoverEveryFactOperationOnBothWorkflowRecords()
        {
            // given: the theories above are driven off the enums, so this only has to prove the
            // derivation SEES everything and lets nothing through that it should not. It
            // deliberately does not count subscriptions — the theories already prove each
            // operation is subscribed AND accepted, by publishing it through the real broker
            // and asserting the round was re-tested.

            // when
            IReadOnlyList<string> reviewFacts =
                FactOperationNamesOf<ApprovalReviewEventOperation>();

            IReadOnlyList<string> commentFacts =
                FactOperationNamesOf<ApprovalCommentEventOperation>();

            // then
            reviewFacts.Should().BeEquivalentTo(
                Enum.GetNames<ApprovalReviewEventOperation>()
                    .Where(name => IsRequestName(name) is false),
                because: "every operation that is not a request is a fact, and §10.17(a) needs " +
                    "every fact address subscribed — so a fact operation added later must " +
                    "arrive already covered by a theory row rather than waiting to be listed");

            reviewFacts.Should().NotContain(name => IsRequestName(name),
                because: "a request is answered by the record's own service, never by the " +
                    "workflow — one leaking in would publish onto an address the workflow does " +
                    "not subscribe to and fail confusingly rather than usefully");

            commentFacts.Should().BeEquivalentTo(
                Enum.GetNames<ApprovalCommentEventOperation>()
                    .Where(name => IsRequestName(name) is false),
                because: "the same holds for comments");

            commentFacts.Should().NotContain(name => IsRequestName(name),
                because: "the same holds for comment requests");
        }

        // A request is a command somebody sends; a fact is what the service publishes afterwards.
        // Named by EXCLUSION rather than by a past-tense suffix, because a fact need not end in
        // "ed" — AssociationEventOperation.ConfidenceSet already does not, so an include-list
        // keyed on "ed" would quietly stop testing a new fact of that shape.
        private static bool IsRequestName(string operationName) =>
            operationName.EndsWith("ing", StringComparison.Ordinal)
                || operationName.EndsWith("ById", StringComparison.Ordinal);

        // The outcomes of the deliveries that went to a subscription the APPROVAL WORKFLOW
        // owns, in order.
        //
        // Filtered to the workflow rather than asserted over every listener, because this
        // collection's fixture is shared and a sibling test may legitimately attach its own
        // probe to the same address. An unfiltered assertion would then fail on a delivery that
        // has nothing to do with what is under test. Filtering keeps BOTH properties that
        // matter: exactly one workflow subscription is reached, and it accepted the fact.
        private static IReadOnlyList<bool> DeliveryOutcomes<T>(
            EventPublishResult<T> publishResult) =>
                (publishResult.Deliveries ?? new List<EventDelivery<T>>())
                    .Where(delivery => EventSubstrateBroker.WorkflowSubscriptionIds
                        .Contains(delivery.SubscriptionId))
                    .Select(delivery => delivery.IsSuccess)
                    .ToList();
    }
}
