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
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what the
    /// design says; these prove the middleware and the foundation act on them — a gate that is
    /// present but never evaluated passes the former and fails here.
    ///
    /// <para>Approval reviews are §14.7 <b>posture D</b>: never public, so every read is gated and
    /// a row the caller may not see is reported as not found rather than refused.</para>
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetAllIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var getAllTask = this.apiBroker.GetAllApprovalReviewsAsync().AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getAllTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetByIdIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var getByIdTask = this.apiBroker.GetApprovalReviewByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getByIdTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview(Guid.NewGuid());
            this.apiBroker.ActAsAnonymous();

            // when
            var postTask = this.apiBroker.PostApprovalReviewAsync(randomApprovalReview).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsNotAdminAsync()
        {
            // given: a reviewer, and then a publisher — neither is Admin
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Publishers);

                var hardDeleteTask =
                    this.apiBroker.HardDeleteApprovalReviewByIdAsync(review.Id).AsTask();

                // then: refused by the coarse attribute, so the row survives
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAs(this.reviewerUserId, Roles.Reviewers);

                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                actualReview.Should().NotBeNull();
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// A read the caller may not see reports not found rather than unauthorized, so the
        /// endpoint cannot be used to probe which verdicts exist (§14.5 rule 1).
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdForAReviewThatDoesNotExistAsync()
        {
            // given, when
            var getByIdTask = this.apiBroker.GetApprovalReviewByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getByIdTask);
        }
    }
}
