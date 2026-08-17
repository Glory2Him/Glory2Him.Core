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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// Dismissal over real HTTP. The gate is the publisher tier matched by SUFFIX, which is why
    /// the controller carries a bare <c>[Authorize]</c> and not a role list — these tests are the
    /// ones that prove the foundation, rather than the attribute, is doing the narrowing.
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldDismissApprovalReviewAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when: a global publisher drives the standing verdict to Dismissed
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Publisher);
                await this.apiBroker.DismissApprovalReviewAsync(review.Id);

                // then
                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                actualReview.StatusId.Should().Be((int)ApprovalStatus.Dismissed);

                // retained rather than deleted (§9.5) — the record of what was said survives
                actualReview.IsDeleted.Should().BeFalse();
                actualReview.Comment.Should().Be(review.Comment);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// A suffix-matched publisher clears the gate, which is the whole reason a fixed
        /// <c>Roles = ...</c> list would have been wrong: this role is not in any enumerable set
        /// the attribute could have named.
        /// </summary>
        [Fact]
        public async Task ShouldDismissApprovalReviewForASuffixMatchedPublisherAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.TagPublisher);
                await this.apiBroker.DismissApprovalReviewAsync(review.Id);

                // then
                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                actualReview.StatusId.Should().Be((int)ApprovalStatus.Dismissed);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// Dismissal is the workflow's act, not the reviewer's (§7.7 rule 2). The author of the
        /// verdict holds a review role and still cannot dismiss it — which is the deadlock #226
        /// records, asserted here rather than merely described.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDismissIfCallerIsTheReviewerAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when: still acting as the reviewer who recorded it
                var dismissTask = this.apiBroker.DismissApprovalReviewAsync(review.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => dismissTask);

                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                actualReview.StatusId.Should().Be((int)ApprovalStatus.Approved);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDismissIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var dismissTask = this.apiBroker.DismissApprovalReviewAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => dismissTask);
        }

        /// <summary>
        /// Refuses a verdict that is already dismissed, so the transition cannot be replayed into
        /// a no-op that still publishes a fact.
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnDismissIfAlreadyDismissedAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Publisher);
                await this.apiBroker.DismissApprovalReviewAsync(review.Id);

                // when
                var secondDismissTask =
                    this.apiBroker.DismissApprovalReviewAsync(review.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => secondDismissTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }
    }
}
