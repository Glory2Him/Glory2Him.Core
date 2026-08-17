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
        /// The re-file route of §7.7 rule 7, and the one case that IS reachable — both the access
        /// predicate and the index filter exclude <c>Dismissed</c>, so a dismissed verdict
        /// releases the slot. This is the live half of the gap recorded in #226: dismissal itself
        /// has to be driven by hand here, because nothing automates it yet.
        /// </summary>
        [Fact]
        public async Task ShouldAllowReFilingAfterTheEarlierReviewIsDismissedAsync()
        {
            // given
            (Approval approval, ApprovalReview firstReview) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            ApprovalReview refiledReview = CreateRandomApprovalReview(approval.Id);

            try
            {
                // a publisher dismisses the standing verdict (design route 3)
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Publisher);
                await this.apiBroker.DismissApprovalReviewAsync(firstReview.Id);

                // when: the original reviewer files again
                this.apiBroker.ActAs(this.reviewerUserId, Roles.Reviewer);
                await this.apiBroker.PostApprovalReviewAsync(refiledReview);

                // then
                ApprovalReview actualRefiledReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(refiledReview.Id);

                actualRefiledReview.Should().NotBeNull();
                actualRefiledReview.CreatedBy.Should().Be(this.reviewerUserId);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(refiledReview.Id);

                await RemoveApprovalReviewAndApprovalAsync(
                    firstReview.Id,
                    approval.Id);
            }
        }
    }
}
