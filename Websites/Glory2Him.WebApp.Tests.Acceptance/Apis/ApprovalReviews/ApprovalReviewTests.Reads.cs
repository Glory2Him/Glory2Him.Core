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
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// The read happy paths. Both are gated (§14.7 posture D) and the collection read applies a
    /// visibility filter rather than refusing: a review-role holder sees every live row, anyone
    /// else sees only their own, and an anonymous caller sees nothing.
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldGetApprovalReviewByIdAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                // then
                actualReview.Should().BeEquivalentTo(review, options => options
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        [Fact]
        public async Task ShouldGetAllApprovalReviewsAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                List<ApprovalReview> actualReviews =
                    await this.apiBroker.GetAllApprovalReviewsAsync();

                // then: the acting caller holds Reviewers, so the row is visible in the collection
                actualReviews.Should().Contain(retrieved => retrieved.Id == review.Id);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// A review role sees every live verdict, not merely its own — reviewing is a shared job,
        /// so unlike the comment thread there is nothing to partition. This asserts the widening
        /// half of the filter; the narrowing half is asserted in the security suite.
        /// </summary>
        [Fact]
        public async Task ShouldSeeAnotherReviewersVerdictInTheCollectionReadAsync()
        {
            // given: a verdict recorded by somebody else entirely
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview seededReview =
                await this.apiBroker.InsertReviewByAsync(approval.Id, Guid.NewGuid().ToString());

            try
            {
                // when
                List<ApprovalReview> actualReviews =
                    await this.apiBroker.GetAllApprovalReviewsAsync();

                // then
                actualReviews.Should().Contain(retrieved => retrieved.Id == seededReview.Id);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(seededReview.Id, approval.Id);
            }
        }
    }
}
