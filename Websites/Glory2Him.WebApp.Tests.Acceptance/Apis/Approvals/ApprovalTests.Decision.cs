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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using RESTFulSense.Exceptions;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    /// <summary>
    /// The generic decision endpoint (§16.7.3) on a CONTENT ITEM — the entity the moderation
    /// screen decides, and the one that had no acceptance coverage of a decision at all: the
    /// per-entity approve tests drive each entity's own transition verb, and nothing drove this
    /// route, which is the only one the React panel calls.
    /// </summary>
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// The ordinary path under the seeded policy: two approving reviews from reviewers who
        /// are not the author, then an administrator applies the outcome. Not a bypass — the
        /// conditions were met, so nothing is waived and nothing is recorded as waived.
        /// </summary>
        [Fact]
        public async Task ShouldApproveAContentItemOnceTheSeededPolicyIsMetAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            ApprovalReview firstReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            ApprovalReview secondReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            try
            {
                // when
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem, submitted.Id, decision: "Approve");

                // then
                actualOutcome.ApprovalId.Should().Be(approval.Id);
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Approved);
                actualOutcome.IsApprovedByBypass.Should().BeFalse();

                // and the round is decided in storage, with §9.8 holding on the item
                Approval storedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                storedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

                CoreContentItem storedContentItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submitted.Id);

                storedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalReviewAsync(secondReview);
                await this.apiBroker.RemoveApprovalReviewAsync(firstReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// The bypass route (§9.7.5): no review at all, the conditions unmet, an administrator
        /// waives them with a reason. What lands on the row is the outcome's to say — the pair is
        /// derived from the decision, never copied from the request.
        /// </summary>
        [Fact]
        public async Task ShouldApproveAContentItemByBypassWithAReasonAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            try
            {
                // when
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem,
                    submitted.Id,
                    decision: "Approve",
                    isBypassRequested: true,
                    bypassReason: "Verified against the printed edition.");

                // then
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Approved);
                actualOutcome.IsApprovedByBypass.Should().BeTrue();
                actualOutcome.ApprovedByBypassReason.Should().Be("Verified against the printed edition.");
            }
            finally
            {
                Approval storedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                await this.apiBroker.RemoveApprovalAsync(storedApproval ?? approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// A bypass without a reason is refused before any policy is read — the reason is what
        /// makes the waiver evidence, not decoration on it.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseABypassWithoutAReasonAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            try
            {
                // when
                var decisionTask = this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem,
                    submitted.Id,
                    decision: "Approve",
                    isBypassRequested: true).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => decisionTask);

                Approval storedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                storedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// A rejection needs no conditions and no bypass (§12.5.3 rule 13): withholding approval
        /// waives nothing.
        /// </summary>
        [Fact]
        public async Task ShouldRejectAContentItemWithNoReviewsAtAllAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            try
            {
                // when
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem, submitted.Id, decision: "Reject");

                // then
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Rejected);
                actualOutcome.IsApprovedByBypass.Should().BeFalse();

                CoreContentItem storedContentItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submitted.Id);

                storedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
            }
            finally
            {
                Approval storedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                await this.apiBroker.RemoveApprovalAsync(storedApproval ?? approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }
    }
}
