// ─────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ─────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using RESTFulSense.Exceptions;
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// §8.6 HR-4's administrator override end to end: an approved, published item is taken
        /// back to <c>Submitted</c>, its reviews are dismissed, and it leaves the public site.
        ///
        /// <para>This is the whole feature in one test, because the three halves are only correct
        /// together — a reset that moved the round but left the reviews standing would be undone
        /// by them, and one that left the item published would recover nothing.</para>
        /// </summary>
        [Fact]
        public async Task ShouldResetAnApprovedItemBackToSubmittedAndTakeItOffTheSiteAsync()
        {
            // given: an item approved on two reviews, and published
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
                await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem, submitted.Id, decision: "Approve");

                CoreContentItem approvedItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submitted.Id);

                approvedItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                approvedItem.IsPublished.Should().BeTrue();

                // when: the administrator takes the outcome back
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalResetAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: the round is open again
                actualOutcome.ApprovalId.Should().Be(approval.Id);
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Submitted);
                actualOutcome.IsApprovedByBypass.Should().BeFalse();

                Approval resetApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                resetApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

                // and §9.8 holds: the item followed, and it is OFF the public site
                CoreContentItem resetItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submitted.Id);

                resetItem.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                resetItem.IsPublished.Should().BeFalse();
                resetItem.PublishDate.Should().BeNull();

                // and every review that produced the overruled verdict is dismissed — §12.5.3
                // rule 12, which had never been implemented before this operation carried it
                ApprovalReview firstAfterReset =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(firstReview.Id);

                ApprovalReview secondAfterReset =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(secondReview.Id);

                firstAfterReset.StatusId.Should().Be(ApprovalStatus.Dismissed);
                secondAfterReset.StatusId.Should().Be(ApprovalStatus.Dismissed);

                // and the round did NOT re-approve itself. The reset moves the entity, the entity
                // publishes -Submitted, and the workflow subscribes to that address — without the
                // echo guard a permissive policy would drive it straight back to Approved in the
                // same request.
                resetApproval.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalReviewAsync(firstReview);
                await this.apiBroker.RemoveApprovalReviewAsync(secondReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// Deciding an open round is the publisher tier's; UNdeciding a closed one is the
        /// override, and the override has one holder (§8.6 HR-4). The publisher is the caller
        /// worth naming: they can reach this panel and apply the outcome, and must not be able to
        /// take it back.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAResetToAPublisherAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), "Publishers");

                // when
                ValueTask<ApprovalOutcome> resetTask =
                    this.apiBroker.PostApprovalResetAsync(EntityType.ContentItem, submitted.Id);

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() =>
                    resetTask.AsTask());
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// A reset undoes an OUTCOME, so a round still open has nothing for it to undo. Refused
        /// rather than quietly rewritten: accepting it would dismiss a live round's reviews for
        /// nothing.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAResetOnARoundThatIsStillOpenAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            ApprovalReview standingReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            try
            {
                // when
                ValueTask<ApprovalOutcome> resetTask =
                    this.apiBroker.PostApprovalResetAsync(EntityType.ContentItem, submitted.Id);

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() =>
                    resetTask.AsTask());

                // and the open round was left entirely alone — its review still counts
                ApprovalReview untouchedReview =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(standingReview.Id);

                untouchedReview.StatusId.Should().Be(ApprovalStatus.Approved);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalReviewAsync(standingReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// THE ECHO GUARD, and the test that earns it. A reset moves the entity, the entity
        /// publishes its <c>-Submitted</c> fact, and the approval workflow subscribes to that
        /// address — so without the guard the round is re-evaluated inside the same request.
        ///
        /// <para>The seeded ContentItem policy cannot show this: it requires two approvals, so a
        /// round whose reviews were just dismissed stays open either way. This arranges the
        /// policy that CAN — a narrow <c>(ContentItem, VerseImage)</c> tier with
        /// <c>RequireApprovals = false</c> and auto-approve on, the shape the seed writes for the
        /// personal association tier — under which an unguarded reset is driven straight back to
        /// <c>Approved</c> and the administrator's override undoes itself.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotReApproveAResetRoundUnderAnAutoApprovingPolicyAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            DateTimeOffset arrangedWhen = DateTimeOffset.UtcNow;

            var autoApprovingPolicy = new CoreApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.VerseImage,
                RequireApprovals = false,
                RequiredNumberOfApprovals = 0,
                AutoApproveIfAllApprovalRequirementsMet = true,
                AllowSelfApproval = false,
                BlockOnReject = false,
                BlockOnZeroApprovalScore = false,
                RequireReapprovalOnChange = false,
                RequireReviewCommentResolutionBeforeApprovals = false,
                DoNotAllowBypassingSettings = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = arrangedWhen,
                UpdatedBy = authorUserId,
                UpdatedWhen = arrangedWhen,
            };

            await this.apiBroker.InsertCoreApprovalSettingAsync(autoApprovingPolicy);

            CoreContentItem submitted = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Submitted,
                isPublished: false,
                authorUserId: authorUserId,
                contentType: ContentType.VerseImage);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.ContentItem, submitted.Id, authorUserId);

            try
            {
                await this.apiBroker.PostApprovalDecisionAsync(
                    EntityType.ContentItem, submitted.Id, decision: "Approve");

                // when
                ApprovalOutcome actualOutcome = await this.apiBroker.PostApprovalResetAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: it STAYS open. Under this policy the conditions are trivially met, so an
                // unguarded re-evaluation would approve it again before the request returned.
                actualOutcome.ApprovalStatus.Should().Be((int)ApprovalStatus.Submitted);

                Approval resetApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                resetApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

                CoreContentItem resetItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submitted.Id);

                resetItem.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                resetItem.IsPublished.Should().BeFalse();
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(autoApprovingPolicy.Id);
            }
        }
    }
}
