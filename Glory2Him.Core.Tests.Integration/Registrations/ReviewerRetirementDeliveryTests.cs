// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// §7.9's two retirements, driven through the REAL substrate (§12.5.4 business rule 4).
    ///
    /// <para><b>Why these exist beside the wiring tests.</b> Those read <c>SubscriptionId</c> and
    /// never <c>IsSuccess</c>, so a receiver that refused every envelope would still look wired —
    /// and the event name and direction are bound INTO the HMAC, so a receiver verifying the
    /// wrong one refuses a genuine envelope silently. These publish against the same integrity
    /// broker the publisher signs with, and assert on what the retirement's workflow seam was
    /// actually ASKED to do. A refused signature, an unbound handler, a colliding subscription id
    /// or a gate that refuses a closed round all record nothing and go red here.</para>
    ///
    /// <para><b>The recording is read AFTER the publish returns</b>, and that is not incidental.
    /// §7.9 rule 8's ordering — the retirement runs BEFORE the entity sync — is a conjunction:
    /// this service writes the outcome before publishing the entity command
    /// (<c>ApprovalOrchestrationServiceTests.ShouldWriteTheOutcomeBeforeTheEntityCommandHasGoneAsync</c>),
    /// AND the subscription on the fact that write publishes is delivered on the publisher's own
    /// call. This file is where the second half is observable: a delivery that had not run by the
    /// time <c>PublishApprovalAsync</c> returned would leave the recording empty.</para>
    ///
    /// <para><b>Rule 8 is also proven end to end over HTTP by the acceptance suite's
    /// <c>ShouldRetirePendingReviewRequestsWhenTheRoundIsDecidedAsync</c>; rule 6 deliberately
    /// has no such counterpart.</b> That asymmetry is a choice rather than a gap. #522's
    /// regression bar makes the acceptance file the one thing in the repository that observes the
    /// retirement being DELIVERED rather than a handler being called, and requires it to stay
    /// green and UNEDITED — an edit to it is read as evidence the behaviour changed. Adding a
    /// rule 6 case there would have to edit it. So rule 6's exposer-level proof is bought here
    /// instead, one layer down but through the same real broker, the same real signing key and
    /// the same real receiver: what the acceptance test adds over this is HTTP and a real
    /// database, and neither is what rule 6's delivery was ever in doubt over.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class ReviewerRetirementDeliveryTests
    {
        private readonly EventSubstrateBroker broker;

        public ReviewerRetirementDeliveryTests(EventSubstrateBroker broker)
        {
            this.broker = broker;
            this.broker.ClearReviewerRetirements();
        }

        [Fact]
        public async Task ShouldRetireTheAnsweredInvitationWhenTheReviewFactIsDeliveredAsync()
        {
            // given: a review recorded by somebody the probe round had invited
            var reviewAdded = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = EventSubstrateBroker.ReviewerProbeApprovalId,
                    CreatedBy = EventSubstrateBroker.ReviewerProbeReviewerUserId,
                }
            };

            // when
            EventPublishResult<ApprovalReview> publishResult =
                await this.broker.EventBroker.PublishApprovalReviewAsync(
                    reviewAdded,
                    ApprovalReviewEventOperation.Added);

            // then: delivered to the reviewer orchestration's own subscription, exactly once and
            // through no other of its two
            EventSubstrateBroker.ReviewerSubscriptionsReached(
                EventSubstrateBroker.SubscriptionsReached(publishResult))
                    .Should().Equal(
                        new[]
                        {
                            EventBrokerIdentifiers
                                .ApprovalReviewerOrchestrationOnApprovalReviewAddedSubscriptionId
                        },
                        because: "ApprovalReview-Added carries §7.9 rule 6's retirement as well "
                            + "as the round's re-test, and the two are separate subscriptions "
                            + "with ids of their own — one id shared between them would collapse "
                            + "to a single registration and silently drop a handler");

            // and ACCEPTED, not merely delivered. The name and the direction are inside the HMAC,
            // so this is what a receiver verifying "ApprovalReviewAdded" as Reply, or verifying
            // some other name, fails on.
            DeliveryOutcomeFor(
                publishResult,
                EventBrokerIdentifiers
                    .ApprovalReviewerOrchestrationOnApprovalReviewAddedSubscriptionId)
                        .Should().BeTrue();

            // and it retired the INVITATION the round was still holding for that person, through
            // the ANSWERED verb — the two verbs bind different DeletionReason sentences, and a
            // round closing on somebody is not the same as them answering
            this.broker.ReviewerRetirements.Should().Equal(
                new[]
                {
                    $"{EventSubstrateBroker.AnsweredRetirement}:"
                        + $"{EventSubstrateBroker.ReviewerProbeAnsweredRequestId}"
                });
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldRetireTheUnansweredInvitationsWhenTheClosedRoundFactIsDeliveredAsync(
            ApprovalStatus closingStatus)
        {
            // given: the round's own outcome write, carrying the closed status inside the HMAC
            var approvalModified = new EventEnvelope<Approval>
            {
                Content = new Approval
                {
                    Id = EventSubstrateBroker.ReviewerProbeApprovalId,
                    EntityType = EntityType.ContentItem,
                    EntityId = Guid.NewGuid(),
                    ApprovalStatus = closingStatus,
                }
            };

            // when
            EventPublishResult<Approval> publishResult =
                await this.broker.EventBroker.PublishApprovalAsync(
                    approvalModified,
                    ApprovalEventOperation.Modified);

            // then
            EventSubstrateBroker.ReviewerSubscriptionsReached(
                EventSubstrateBroker.SubscriptionsReached(publishResult))
                    .Should().Equal(
                        new[]
                        {
                            EventBrokerIdentifiers
                                .ApprovalReviewerOrchestrationOnApprovalModifiedSubscriptionId
                        },
                        because: "Approval-Modified is the first of the Approval entity's own "
                            + "FACT addresses to carry a subscription — the five existing "
                            + "registrations all bind command addresses");

            DeliveryOutcomeFor(
                publishResult,
                EventBrokerIdentifiers
                    .ApprovalReviewerOrchestrationOnApprovalModifiedSubscriptionId)
                        .Should().BeTrue();

            // ALREADY DONE by the time the publish returned, with no wait and no poll. That is
            // the synchronous-delivery half of §7.9 rule 8's ordering: the reaction lands inside
            // ModifyApprovalAsync, and therefore ahead of PublishEntityApprovalCommandAsync.
            this.broker.ReviewerRetirements.Should().Equal(
                new[]
                {
                    $"{EventSubstrateBroker.ClosedRoundRetirement}:"
                        + $"{EventSubstrateBroker.ReviewerProbeUnansweredRequestId}"
                });
        }

        /// <summary>
        /// §12.5.4 business rule 4(i) against a REAL publisher. The gate reads the status out of
        /// the signed content, so it is worth proving once where the content really was signed
        /// rather than handed to the handler by a test.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheDeliveredRoundIsStillOpenAsync()
        {
            // given: §8.6 HR-4's reset writes Submitted and publishes this same fact
            var approvalModified = new EventEnvelope<Approval>
            {
                Content = new Approval
                {
                    Id = EventSubstrateBroker.ReviewerProbeApprovalId,
                    EntityType = EntityType.ContentItem,
                    EntityId = Guid.NewGuid(),
                    ApprovalStatus = ApprovalStatus.Submitted,
                }
            };

            // when
            EventPublishResult<Approval> publishResult =
                await this.broker.EventBroker.PublishApprovalAsync(
                    approvalModified,
                    ApprovalEventOperation.Modified);

            // then: DELIVERED and accepted — asserted so this cannot pass because the
            // subscription was never reached, which is the shape an empty recording alone has
            DeliveryOutcomeFor(
                publishResult,
                EventBrokerIdentifiers
                    .ApprovalReviewerOrchestrationOnApprovalModifiedSubscriptionId)
                        .Should().BeTrue();

            // and it retired nothing, because the round it describes is still open
            this.broker.ReviewerRetirements.Should().BeEmpty(
                because: "an ungated sweep would take the invitations a moderator had just "
                    + "re-issued after §8.6 HR-4's reset");
        }

        // The success flag for ONE subscription's delivery. Read per subscription rather than
        // across the whole publish, because ApprovalReview-Added also reaches the round's re-test
        // and that handler's outcome is a different test's subject.
        private static bool? DeliveryOutcomeFor<T>(
            EventPublishResult<T> publishResult,
            Guid subscriptionId) =>
                (publishResult.Deliveries ?? new List<EventDelivery<T>>())
                    .Where(delivery => delivery.SubscriptionId == subscriptionId)
                    .Select(delivery => (bool?)delivery.IsSuccess)
                    .SingleOrDefault();
    }
}
