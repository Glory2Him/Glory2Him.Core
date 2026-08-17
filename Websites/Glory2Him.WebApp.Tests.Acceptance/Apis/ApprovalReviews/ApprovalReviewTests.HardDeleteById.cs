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
    /// Permanent removal, the one operation with a fixed enumerable tier in the attribute. It
    /// destroys the row rather than retaining it, which is why it is <c>Admin</c> alone and why
    /// §7.7 records it as a route rather than endorsing it.
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldHardDeleteApprovalReviewByIdAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when: the seeded administrator carries the real Admin role
                this.apiBroker.ActAsSeededAdministrator();

                ApprovalReview hardDeletedReview =
                    await this.apiBroker.HardDeleteApprovalReviewByIdAsync(review.Id);

                // then
                hardDeletedReview.Id.Should().Be(review.Id);

                // gone from storage, not merely flagged — this is what distinguishes it from the
                // owner-only soft delete
                Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview storedReview =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(review.Id);

                storedReview.Should().BeNull();
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// The global block role wins over <c>Admin</c>. A caller carrying both is refused, so a
        /// blocked administrator cannot destroy a verdict — the same precedence the comment
        /// exposer asserts, and the reason the block is checked before any tier.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                this.apiBroker.ActAs(
                    Guid.NewGuid().ToString(),
                    Roles.Admin,
                    Roles.ReadOnly);

                var hardDeleteTask =
                    this.apiBroker.HardDeleteApprovalReviewByIdAsync(review.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);

                // and the row survives
                this.apiBroker.ActAsSeededAdministrator();

                Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview storedReview =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(review.Id);

                storedReview.Should().NotBeNull();
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }
    }
}
