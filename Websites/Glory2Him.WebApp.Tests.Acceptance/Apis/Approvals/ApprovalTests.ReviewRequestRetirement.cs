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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    /// <summary>
    /// §7.9 rule 8 end to end — the panel that showed "Requested" beside a settled outcome is
    /// what this is for, so the assertion that matters is the one made through the read that
    /// panel calls.
    /// </summary>
    public partial class ApprovalApiTests
    {
        [Fact]
        public async Task ShouldRetirePendingReviewRequestsWhenTheRoundIsDecidedAsync()
        {
            // given: a round with two people asked and neither having answered
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            ApprovalReview firstReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            ApprovalReview secondReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            ApprovalReviewRequest firstRequest =
                await this.apiBroker.InsertPendingReviewRequestAsync(
                    approval.Id, Guid.NewGuid().ToString());

            ApprovalReviewRequest secondRequest =
                await this.apiBroker.InsertPendingReviewRequestAsync(
                    approval.Id, Guid.NewGuid().ToString());

            try
            {
                // and the panel does show them while the round is open, so what changes below is
                // observed rather than assumed from an empty list that was always empty
                List<ApprovalReviewRequest> outstandingBeforeDecision =
                    await this.apiBroker.GetApprovalReviewRequestsAsync(
                        EntityType.ContentItem, submitted.Id);

                outstandingBeforeDecision.Should().HaveCount(2);

                // when
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem, submitted.Id, decision: "Approve");

                // then
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Approved);

                List<ApprovalReviewRequest> outstandingAfterDecision =
                    await this.apiBroker.GetApprovalReviewRequestsAsync(
                        EntityType.ContentItem, submitted.Id);

                outstandingAfterDecision.Should().BeEmpty(
                    because: "a decided round is waiting on nobody, and a review can no longer "
                        + "be recorded against it at all");

                // RETIRED, not deleted. The row survives with the sentence that says which of the
                // three ends it met, which is the whole reason the close has its own verb.
                ApprovalReviewRequest storedFirstRequest =
                    await this.apiBroker.GetCoreApprovalReviewRequestByIdAsync(firstRequest.Id);

                ApprovalReviewRequest storedSecondRequest =
                    await this.apiBroker.GetCoreApprovalReviewRequestByIdAsync(secondRequest.Id);

                storedFirstRequest.IsDeleted.Should().BeTrue();
                storedSecondRequest.IsDeleted.Should().BeTrue();

                storedFirstRequest.DeletionReason.Should()
                    .Be("Retired: the approval round closed before this review was cast.");

                storedSecondRequest.DeletionReason.Should()
                    .Be(storedFirstRequest.DeletionReason);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalReviewRequestAsync(secondRequest);
                await this.apiBroker.RemoveApprovalReviewRequestAsync(firstRequest);
                await this.apiBroker.RemoveApprovalReviewAsync(secondReview);
                await this.apiBroker.RemoveApprovalReviewAsync(firstReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }
    }
}
