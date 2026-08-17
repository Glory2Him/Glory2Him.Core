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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// The rest of the posture: every write verb turned away for an anonymous caller, the global
    /// block role beating every tier, and the narrowing half of the collection read filter.
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsAnonymousAsync()
        {
            // given
            ApprovalReview randomReview = CreateRandomApprovalReview(Guid.NewGuid());
            this.apiBroker.ActAsAnonymous();

            // when
            var putTask = this.apiBroker.PutApprovalReviewAsync(randomReview).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var deleteTask =
                this.apiBroker.DeleteApprovalReviewByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var hardDeleteTask =
                this.apiBroker.HardDeleteApprovalReviewByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
        }

        /// <summary>
        /// The global block role is checked before any tier, so it beats a review role outright.
        /// An <c>ApprovalReview</c> has no entity-scoped ReadOnly of its own — a verdict is
        /// workflow bookkeeping rather than contributed content — so only the global one applies.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAReviewRoleAsync()
        {
            // given
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalReview randomReview = CreateRandomApprovalReview(approval.Id);

            try
            {
                // when
                this.apiBroker.ActAs(
                    Guid.NewGuid().ToString(),
                    Roles.Reviewer,
                    Roles.ReadOnly);

                var postTask = this.apiBroker.PostApprovalReviewAsync(randomReview).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(randomReview.Id, approval.Id);
            }
        }

        /// <summary>
        /// The blocked caller cannot dismiss either, even holding the publisher tier the gate
        /// otherwise requires.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDismissIfBlockedCallerAlsoHoldsPublisherAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                this.apiBroker.ActAs(
                    Guid.NewGuid().ToString(),
                    Roles.Publisher,
                    Roles.ReadOnly);

                var dismissTask = this.apiBroker.DismissApprovalReviewAsync(review.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => dismissTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// Posture D on the single read: a caller holding no review role is told not-found rather
        /// than refused, so the endpoint cannot be used to probe which verdicts exist (§14.5
        /// rule 1) — even though the row is plainly there.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfCallerHoldsNoReviewRoleAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                this.apiBroker.ActAsContributor();

                var getByIdTask = this.apiBroker.GetApprovalReviewByIdAsync(review.Id).AsTask();

                // then: not-found, NOT unauthorized — that distinction is the posture
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getByIdTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// The narrowing half of the collection filter: a caller holding no review role sees only
        /// their own verdicts, so somebody else's is filtered out rather than refused. The read
        /// degrades instead of failing, which is what keeps the collection usable for a
        /// contributor while still not leaking.
        /// </summary>
        [Fact]
        public async Task ShouldFilterOtherReviewersVerdictsOutOfTheCollectionReadAsync()
        {
            // given
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview seededReview =
                await this.apiBroker.InsertReviewByAsync(approval.Id, Guid.NewGuid().ToString());

            try
            {
                // when
                this.apiBroker.ActAsContributor();

                List<ApprovalReview> actualReviews =
                    await this.apiBroker.GetAllApprovalReviewsAsync();

                // then
                actualReviews.Should().NotContain(retrieved => retrieved.Id == seededReview.Id);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(seededReview.Id, approval.Id);
            }
        }

        /// <summary>
        /// An anonymous collection read is refused outright rather than degrading to an empty set,
        /// because the coarse <c>[Authorize]</c> stops it before the filter is reached. The
        /// filter's own anonymous branch is therefore defence in depth, not the live path.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseTheCollectionReadBeforeTheFilterForAnAnonymousCallerAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var getAllTask = this.apiBroker.GetAllApprovalReviewsAsync().AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getAllTask);
        }
    }
}
