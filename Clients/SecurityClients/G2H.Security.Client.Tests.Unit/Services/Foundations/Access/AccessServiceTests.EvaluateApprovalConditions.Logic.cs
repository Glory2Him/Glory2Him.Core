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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldMeetConditionsTriviallyWhenApprovalsAreNotRequiredAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                requiredNumberOfApprovals: 7);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
            actualVerdict.ApprovalCount.Should().Be(0);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(0);

            actualVerdict.Explanation.Should()
                .Be("Conditions trivially met: the policy does not require approvals.");
        }

        [Fact]
        public async Task ShouldMeetConditionsWhenTheApprovalThresholdIsMetExactlyAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
            actualVerdict.ApprovalCount.Should().Be(2);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);

            actualVerdict.Explanation.Should()
                .Be("Conditions met with 2 of 2 required approvals.");
        }

        [Fact]
        public async Task ShouldNotMeetConditionsWhenTheApprovalThresholdIsShortByOneAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.ShouldAutoApprove.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.ApprovalThresholdNotMet);
            actualVerdict.ApprovalCount.Should().Be(1);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);

            actualVerdict.Explanation.Should()
                .Be("1 of 2 required approvals recorded.");
        }

        [Fact]
        public async Task ShouldNotCountDismissedReviewsTowardTheApprovalThresholdAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Dismissed),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.ApprovalThresholdNotMet);
            actualVerdict.ApprovalCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotCountSoftDeletedReviewsTowardTheApprovalThresholdAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),

                CreateRandomReviewRecord(
                    verdict: ReviewVerdict.Approved,
                    isDeleted: true),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.ApprovalThresholdNotMet);
            actualVerdict.ApprovalCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotMeetConditionsWhenAnActiveRejectionIsPresentAndRejectionsBlockAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                blockOnReject: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Rejected),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.BlockedByRejection);
            actualVerdict.ApprovalCount.Should().Be(1);

            actualVerdict.Explanation.Should()
                .Be("An active rejection blocks this approval.");
        }

        // The §8.5 worked example: two approvals required, one reject that does not block,
        // two approves recorded, so the conditions are met with the rejection standing.
        [Fact]
        public async Task ShouldMeetConditionsWithARejectionPresentWhenRejectionsDoNotBlockAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2,
                blockOnReject: false);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Rejected),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
            actualVerdict.ApprovalCount.Should().Be(2);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);
        }

        [Fact]
        public async Task ShouldNotMeetConditionsWhenAnApprovalCommentIsUnresolvedAndResolutionIsRequiredAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                requireReviewCommentResolutionBeforeApprovals: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            var comments = new List<CommentRecord>
            {
                CreateRandomCommentRecord(isResolved: false),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    comments: comments);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();

            actualVerdict.BlockReason.Should()
                .Be(AccessDenialReason.BlockedByUnresolvedComment);

            actualVerdict.Explanation.Should()
                .Be("An approval comment is still unresolved.");
        }

        [Fact]
        public async Task ShouldMeetConditionsWithAnUnresolvedCommentWhenResolutionIsNotRequiredAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                requireReviewCommentResolutionBeforeApprovals: false);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            var comments = new List<CommentRecord>
            {
                CreateRandomCommentRecord(isResolved: false),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    comments: comments);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldNotBlockOnASoftDeletedUnresolvedCommentAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                requireReviewCommentResolutionBeforeApprovals: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            var comments = new List<CommentRecord>
            {
                CreateRandomCommentRecord(isResolved: false, isDeleted: true),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    comments: comments);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldNotMeetConditionsOnAZeroConfidenceScoreWhenZeroScoresBlockAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                blockOnZeroApprovalScore: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    confidenceScore: 0m);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();

            actualVerdict.BlockReason.Should()
                .Be(AccessDenialReason.BlockedByZeroConfidenceScore);

            actualVerdict.Explanation.Should()
                .Be("The entity's confidence score is zero.");
        }

        [Fact]
        public async Task ShouldMeetConditionsOnAZeroConfidenceScoreWhenZeroScoresDoNotBlockAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                blockOnZeroApprovalScore: false);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    confidenceScore: 0m);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
        }

        // §8.5 rule 8. A null score means the confidence process has not run, not that the
        // entity scored nothing, so it must never block however the flag is set.
        [Fact]
        public async Task ShouldNeverBlockOnANullConfidenceScoreEvenWhenZeroScoresBlockAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                blockOnZeroApprovalScore: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy,
                    reviews: reviews,
                    confidenceScore: null);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldAutoApproveWhenConditionsAreMetAndAutoApprovalIsConfiguredAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                autoApproveIfAllApprovalRequirementsMet: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.ShouldAutoApprove.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldNotAutoApproveWhenConditionsAreMetButAutoApprovalIsNotConfiguredAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                autoApproveIfAllApprovalRequirementsMet: false);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.ShouldAutoApprove.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotAutoApproveWhenAutoApprovalIsConfiguredButConditionsAreNotMetAsync()
        {
            // given
            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2,
                autoApproveIfAllApprovalRequirementsMet: true);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(approvalPolicy, reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.ShouldAutoApprove.Should().BeFalse();
        }
    }
}
