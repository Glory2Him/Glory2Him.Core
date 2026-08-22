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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;
using RESTFulSense.Exceptions;
using CoreApprovalReview = Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview;
using CoreApprovalStatus = Glory2Him.Core.Models.Enums.ApprovalStatus;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;
using Tag = Glory2Him.WebApp.Tests.Acceptance.Models.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldPostApprovalReviewAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalReview inputApprovalReview = CreateRandomApprovalReview(randomApproval.Id);

            try
            {
                // when
                await this.apiBroker.PostApprovalReviewAsync(inputApprovalReview);

                ApprovalReview actualApprovalReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(inputApprovalReview.Id);

                // then
                actualApprovalReview.Should().BeEquivalentTo(inputApprovalReview, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));

                // the verdict is bound to the acting reviewer, never to the payload
                actualApprovalReview.CreatedBy.Should().Be(this.reviewerUserId);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(
                    inputApprovalReview.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// §8.9: only reviewers review. A bare authenticated caller holding no role at all is
        /// refused, which is where this exposer differs most sharply from the comment one — there,
        /// any authenticated caller may join the thread.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerHoldsNoReviewRoleAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalReview randomApprovalReview = CreateRandomApprovalReview(randomApproval.Id);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalReviewAsync(randomApprovalReview).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(
                    randomApprovalReview.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The parent round is not decoration. A verdict aimed at an approval that does not exist
        /// is refused at the access gate rather than left to the foreign key, so the caller gets
        /// an authorization answer and no row is written (§7.7 rule 1).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfParentApprovalDoesNotExistAsync()
        {
            // given
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview(Guid.NewGuid());

            // when
            var postTask =
                this.apiBroker.PostApprovalReviewAsync(randomApprovalReview).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
        }

        /// <summary>
        /// §7.7 rule 1 — one ACTIVE review per reviewer per approval. The mapping table gives a
        /// 409 for the unique index, but that is unreachable over HTTP: the access decision
        /// returns <c>ActiveReviewAlreadyRecorded</c> before the insert runs, which surfaces as
        /// <b>401</b>. Only a genuine concurrent race reaches the index, and a sequential test
        /// cannot drive one — so this asserts 401, and the 409 is pinned in the unit suite.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfTheReviewerAlreadyHasAnActiveReviewAsync()
        {
            // given
            (Approval approval, ApprovalReview firstReview) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            ApprovalReview secondReview = CreateRandomApprovalReview(approval.Id);

            try
            {
                // when
                var postTask = this.apiBroker.PostApprovalReviewAsync(secondReview).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(secondReview.Id);

                await RemoveApprovalReviewAndApprovalAsync(
                    firstReview.Id,
                    approval.Id);
            }
        }

        /// <summary>
        /// §7.7 rule 7's re-file route, end to end — and the ONLY test that exercises the live
        /// unique index against a real dismissal (#301).
        /// </summary>
        /// <remarks>
        /// <para>An earlier version of this test set its precondition by calling the dismiss
        /// endpoint as a publisher. #295 removed that route, so the precondition now comes from
        /// the real trigger: the author edits the content, the orchestration hears
        /// <c>Tag-Modified</c>, and it dismisses the stale review under the system identity.
        /// Substrate delivery is synchronous and in-process, so that completes inside the PUT
        /// rather than needing a poll.</para>
        ///
        /// <para><b>Why it is worth the setup.</b> The index carries
        /// <c>StatusId &lt;&gt; Dismissed AND IsDeleted = 0</c>, and that predicate is asserted
        /// at model and broker level — but only this test proves the DATABASE agrees. With it
        /// deleted, dropping the term from the live index left the whole acceptance suite
        /// green.</para>
        /// </remarks>
        [Fact]
        public async Task ShouldAllowReFilingAfterAContentEditDismissesTheEarlierReviewAsync()
        {
            // given: a submitted tag, its open round, and a standing approval on it
            string authorUserId = Guid.NewGuid().ToString();
            string reviewerUserId = Guid.NewGuid().ToString();

            CoreTag submittedTag =
                await this.apiBroker.InsertSubmittedTagAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(submittedTag.Id, authorUserId);

            CoreApprovalReview firstReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, reviewerUserId);

            ApprovalReview refiledReview = CreateRandomApprovalReview(approval.Id);

            try
            {
                // when: the AUTHOR edits the content the review judged
                this.apiBroker.ActAs(authorUserId);
                Tag tagToEdit = await this.apiBroker.GetTagByIdAsync(submittedTag.Id);
                // Replaced rather than appended: Name is capped at 30 and the arrangement
                // already uses all 30.
                tagToEdit.Name = Guid.NewGuid().ToString("N").Substring(0, 30);

                // The modify gate refuses a stale stamp, so the edit carries a fresh one the
                // way a real client's would.
                tagToEdit.UpdatedWhen = DateTimeOffset.UtcNow;

                await this.apiBroker.PutTagAsync(tagToEdit);

                // then: the workflow dismissed the standing verdict, and no human did
                CoreApprovalReview dismissedReview =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(firstReview.Id);

                dismissedReview.StatusId.Should().Be(CoreApprovalStatus.Dismissed,
                    because: "a content change invalidates the reviews of the text it "
                        + "replaced (§9.7.4), and only the workflow can record that");

                // and: the slot the dismissed row held is released, so the SAME reviewer may
                // file afresh — which is the half the unique index decides
                this.apiBroker.ActAs(reviewerUserId, Roles.Reviewer);
                await this.apiBroker.PostApprovalReviewAsync(refiledReview);

                ApprovalReview actualRefiledReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(refiledReview.Id);

                actualRefiledReview.Should().NotBeNull();
                actualRefiledReview.CreatedBy.Should().Be(reviewerUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(refiledReview.Id);
                await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(firstReview.Id);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreTagByIdAsync(submittedTag.Id);
            }
        }
    }
}
