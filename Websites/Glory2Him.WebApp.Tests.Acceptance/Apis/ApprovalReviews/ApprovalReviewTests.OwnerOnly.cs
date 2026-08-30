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
    /// A verdict belongs to the reviewer who recorded it. Modify and soft removal are the owner
    /// alone — not <c>Publisher</c>, not <c>Admin</c> (§14.7 rule 4) — and an Admin who needs past
    /// a standing rejection bypasses the block rather than editing the review out of the way,
    /// which keeps the record of what was actually said intact.
    ///
    /// <para>These need a review written by SOMEONE ELSE, which only storage can arrange: the API
    /// binds <c>CreatedBy</c> to the acting caller.</para>
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldPutOwnApprovalReviewAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            review.Comment = GetRandomComment();
            review.StatusId = (int)ApprovalStatus.Rejected;

            try
            {
                // when
                await this.apiBroker.PutApprovalReviewAsync(review);

                ApprovalReview actualReview =
                    await this.apiBroker.GetApprovalReviewByIdAsync(review.Id);

                // then
                actualReview.Comment.Should().Be(review.Comment);
                actualReview.StatusId.Should().Be((int)ApprovalStatus.Rejected);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.Administrators)]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheReviewerAsync(string callerRole)
        {
            // given: a verdict recorded by somebody else
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            var strangerUserId = Guid.NewGuid().ToString();

            Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview seededReview =
                await this.apiBroker.InsertReviewByAsync(approval.Id, strangerUserId);

            try
            {
                // when
                string callerUserId = this.apiBroker.ActAs(
                    Guid.NewGuid().ToString(),
                    callerRole is null ? Array.Empty<string>() : new[] { callerRole });

                // Structurally VALID, so the run reaches the ownership gate rather than stopping
                // at the shape rules — the audit members are carried over from the stored row and
                // only the verdict and its wording are tampered with. A payload with blank dates
                // is refused at 400 before ownership is ever consulted, which would make this
                // test pass while proving nothing about who may edit a verdict.
                var tamperedReview = new ApprovalReview
                {
                    Id = seededReview.Id,
                    ApprovalId = approval.Id,
                    StatusId = (int)ApprovalStatus.Rejected,
                    Comment = GetRandomComment(),
                    IsDeleted = false,
                    CreatedBy = seededReview.CreatedBy,
                    CreatedWhen = seededReview.CreatedWhen,
                    UpdatedBy = callerUserId,
                    UpdatedWhen = DateTimeOffset.UtcNow,
                };

                var putTask = this.apiBroker.PutApprovalReviewAsync(tamperedReview).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(seededReview.Id, approval.Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.Administrators)]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNotTheReviewerAsync(string callerRole)
        {
            // given
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            var strangerUserId = Guid.NewGuid().ToString();

            Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview seededReview =
                await this.apiBroker.InsertReviewByAsync(approval.Id, strangerUserId);

            try
            {
                // when
                this.apiBroker.ActAs(
                    Guid.NewGuid().ToString(),
                    callerRole is null ? Array.Empty<string>() : new[] { callerRole });

                var deleteTask =
                    this.apiBroker.DeleteApprovalReviewByIdAsync(seededReview.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteTask);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(seededReview.Id, approval.Id);
            }
        }

        [Fact]
        public async Task ShouldDeleteOwnApprovalReviewAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            try
            {
                // when
                await this.apiBroker.DeleteApprovalReviewByIdAsync(review.Id);

                // then: soft-deleted, so the read reports not found (§14.5)
                var getTask = this.apiBroker.GetApprovalReviewByIdAsync(review.Id).AsTask();
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getTask);

                // and it is filtered out of the collection too, asserted through the OData
                // $filter route — the only test that exercises [EnableQuery] on this controller,
                // and the same check the sibling exposer makes here
                List<ApprovalReview> filteredResult =
                    await this.apiBroker.GetSpecificApprovalReviewByIdAsync(review.Id);

                filteredResult.Count.Should().Be(0);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

        /// <summary>
        /// The reason rides the query string, so only a request through the real pipeline proves it
        /// binds and reaches the column. The unit test passes it as a direct method argument and
        /// therefore cannot: model binding is skipped entirely when the action is invoked in
        /// process (exposer skill §1.8 item 3).
        /// </summary>
        [Fact]
        public async Task ShouldCarryTheDeletionReasonThroughToStorageOnDeleteAsync()
        {
            // given
            (Approval approval, ApprovalReview review) =
                await PostRandomApprovalReviewOnOpenApprovalAsync();

            string deletionReason = "withdrawn by the reviewer, reason bound over the wire";

            try
            {
                // when
                await this.apiBroker.DeleteApprovalReviewByIdAsync(review.Id, deletionReason);

                // then: read beneath HTTP, because the endpoint now reports the row as not found
                Glory2Him.Core.Models.Foundations.ApprovalReviews.ApprovalReview storedReview =
                    await this.apiBroker.GetCoreApprovalReviewByIdAsync(review.Id);

                storedReview.IsDeleted.Should().BeTrue();
                storedReview.DeletionReason.Should().Be(deletionReason);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(review.Id, approval.Id);
            }
        }

    }
}
