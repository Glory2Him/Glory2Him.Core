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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// Answers issue #298: when a subscription handler throws, does the substrate CONTAIN the
    /// failure as an unsuccessful delivery, or does it propagate out of the publish and fail the
    /// publisher?
    ///
    /// <para><b>Why it has to be measured rather than reasoned about.</b> Delivery is synchronous
    /// and in-process, on the publisher's own execution context, so nothing about the shape of
    /// the code says which it is. The answer decides how every publisher in the solution must be
    /// written. If failures are contained, a publisher that discards its <c>EventPublishResult</c>
    /// silently loses them, and the fix is to inspect the result. If they propagate, a publisher
    /// is one throwing subscriber away from failing a request whose own work already committed —
    /// a reviewer's vote is written, then their POST returns 500 because a bookkeeping row would
    /// not delete.</para>
    ///
    /// <para>Both are defensible designs. Only one is true, and until this test existed the
    /// codebase contained comments asserting each.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class HandlerFailureContainmentTests
    {
        private readonly EventSubstrateBroker broker;

        public HandlerFailureContainmentTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        [Fact]
        public async Task ShouldContainAThrowingHandlerRatherThanFailThePublisherAsync()
        {
            // given: a handler that fails part-way through real work, which is what a storage
            // outage or a validation refusal inside the re-test looks like from here
            var handlerFailure = new InvalidOperationException("the handler failed");
            this.broker.HandlerException = handlerFailure;

            try
            {
                EventPublishResult<ApprovalReview> publishResult = null;

                // when
                Func<Task> publishing = async () =>
                    publishResult = await this.broker.EventBroker.PublishApprovalReviewAsync(
                        new EventEnvelope<ApprovalReview>
                        {
                            Content = new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = Guid.NewGuid()
                            }
                        },
                        ApprovalReviewEventOperation.Added);

                // then: the publish itself completes. A throwing subscriber must not be able to
                // fail the request that published the fact — the reviewer's vote is already
                // committed by this point, and a 500 here would report a write that succeeded
                // as one that did not.
                await publishing.Should().NotThrowAsync(
                    because: "delivery is synchronous on the publisher's context, so an " +
                        "uncontained handler failure would surface as the PUBLISHER's failure");

                publishResult.Should().NotBeNull();

                // and: the failure is not silently swallowed either — it is reported on the
                // delivery, which is the only place a publisher can see it. A publisher that
                // discards this result loses the failure entirely, and that is the shape every
                // caller in the solution has to be written against.
                IReadOnlyList<EventDelivery<ApprovalReview>> deliveries =
                    publishResult.Deliveries ?? new List<EventDelivery<ApprovalReview>>();

                deliveries.Should().NotBeEmpty(
                    because: "the subscription IS reached — the failure happens inside it, not " +
                        "before it");

                deliveries.Should().Contain(delivery => delivery.IsSuccess == false,
                    because: "a handler that threw did not succeed, and a delivery reported " +
                        "successful would hide the failure from the only caller placed to see " +
                        "it");

                // Deliberately NOT asserting that sibling subscribers survived. ApprovalReview-
                // Added binds exactly ONE subscription, so there are no siblings to survive, and
                // an earlier version of this test asserted two deliveries because a local
                // substrate database still carried a subscription row from a previous run - the
                // "fire twice per edit" shape WorkflowRecordFactTests exists to catch. CI, with a
                // fresh store, was right and the assertion was encoding a dirty environment.
                //
                // Containment is what #298 asked and what this test answers: the publish
                // completes, and the failure is reported on the delivery rather than thrown.
            }
            finally
            {
                this.broker.HandlerException = null;
            }
        }
    }
}
