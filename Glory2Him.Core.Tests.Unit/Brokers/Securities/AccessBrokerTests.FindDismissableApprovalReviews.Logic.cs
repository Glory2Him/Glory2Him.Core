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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // The gather the changed-reaction flow reads (§APR9.7.4). Unfiltered by the caller, so it
        // has no caller of its own to refuse: every test here is the happy path, and each one
        // executes the query-shaping function the broker hands down over an in-memory set.
        [Fact]
        public async Task ShouldGatherEachActiveReviewWithWhenItWasWrittenAndWhetherItRejectsAsync()
        {
            // given: two active reviews written at different moments, so a projection that
            // swapped or dropped CreatedWhen cannot pass
            Guid approvalId = Guid.NewGuid();
            DateTimeOffset firstCreatedWhen = DateTimeOffset.UtcNow.AddHours(-2);
            DateTimeOffset secondCreatedWhen = DateTimeOffset.UtcNow.AddHours(-1);

            ApprovalReview approvedReview = CreateDismissableApprovalReview(
                approvalId: approvalId,
                statusId: ApprovalStatus.Approved,
                createdWhen: firstCreatedWhen);

            ApprovalReview submittedReview = CreateDismissableApprovalReview(
                approvalId: approvalId,
                statusId: ApprovalStatus.Submitted,
                createdWhen: secondCreatedWhen);

            SetupDismissableApprovalReviewsQueryOver(approvedReview, submittedReview);

            var expectedDismissableApprovalReviews = new List<DismissableApprovalReview>
            {
                new DismissableApprovalReview
                {
                    Id = approvedReview.Id,
                    CreatedWhen = firstCreatedWhen,
                    IsRejection = false,
                },
                new DismissableApprovalReview
                {
                    Id = submittedReview.Id,
                    CreatedWhen = secondCreatedWhen,
                    IsRejection = false,
                },
            };

            // when
            IReadOnlyList<DismissableApprovalReview> actualDismissableApprovalReviews =
                await this.accessBroker.FindDismissableApprovalReviewsAsync(
                    approvalId: approvalId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualDismissableApprovalReviews.Should().BeEquivalentTo(
                expectedDismissableApprovalReviews);

            VerifyDismissableApprovalReviewsQueriedOnce();
        }

        [Fact]
        public async Task ShouldGatherOnlyTheRoundsOwnActiveReviewsAsync()
        {
            // given: the round's one active review, and beside it three rows that each miss on
            // exactly one term, so dropping any term lets its row through
            Guid approvalId = Guid.NewGuid();
            DateTimeOffset createdWhen = DateTimeOffset.UtcNow.AddHours(-1);

            ApprovalReview activeReview = CreateDismissableApprovalReview(
                approvalId: approvalId,
                statusId: ApprovalStatus.Approved,
                createdWhen: createdWhen);

            // Same round, withdrawn: §9.5 keeps the row for audit, not for counting.
            ApprovalReview softDeletedReview = CreateDismissableApprovalReview(
                approvalId: approvalId,
                statusId: ApprovalStatus.Approved,
                createdWhen: createdWhen,
                isDeleted: true);

            // Same round, already dismissed: dismissing it again would throw at the transition.
            ApprovalReview dismissedReview = CreateDismissableApprovalReview(
                approvalId: approvalId,
                statusId: ApprovalStatus.Dismissed,
                createdWhen: createdWhen);

            // Another round entirely: without this term one reader's change would reach every
            // round's reviews in the table.
            ApprovalReview otherRoundsReview = CreateDismissableApprovalReview(
                approvalId: Guid.NewGuid(),
                statusId: ApprovalStatus.Approved,
                createdWhen: createdWhen);

            SetupDismissableApprovalReviewsQueryOver(
                activeReview,
                softDeletedReview,
                dismissedReview,
                otherRoundsReview);

            var expectedDismissableApprovalReviews = new List<DismissableApprovalReview>
            {
                new DismissableApprovalReview
                {
                    Id = activeReview.Id,
                    CreatedWhen = createdWhen,
                    IsRejection = false,
                },
            };

            // when
            IReadOnlyList<DismissableApprovalReview> actualDismissableApprovalReviews =
                await this.accessBroker.FindDismissableApprovalReviewsAsync(
                    approvalId: approvalId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualDismissableApprovalReviews.Should().BeEquivalentTo(
                expectedDismissableApprovalReviews);

            VerifyDismissableApprovalReviewsQueriedOnce();
        }

        [Fact]
        public async Task ShouldMarkOnlyARejectedReviewAsARejectionAsync()
        {
            // given: one active review in every status the gather returns, so a rejection test
            // that matched any status but Rejected — or keyed on "not Approved" — marks a row
            // it should not. Dismissed is not returned at all (see the test above).
            Guid approvalId = Guid.NewGuid();
            DateTimeOffset createdWhen = DateTimeOffset.UtcNow.AddHours(-1);

            List<ApprovalReview> storageApprovalReviews = Enum.GetValues<ApprovalStatus>()
                .Where(statusId => statusId != ApprovalStatus.Dismissed)
                .Select(statusId => CreateDismissableApprovalReview(
                    approvalId: approvalId,
                    statusId: statusId,
                    createdWhen: createdWhen))
                .ToList();

            SetupDismissableApprovalReviewsQueryOver(storageApprovalReviews.ToArray());

            List<DismissableApprovalReview> expectedDismissableApprovalReviews =
                storageApprovalReviews
                    .Select(approvalReview => new DismissableApprovalReview
                    {
                        Id = approvalReview.Id,
                        CreatedWhen = createdWhen,
                        IsRejection = approvalReview.StatusId == ApprovalStatus.Rejected,
                    })
                    .ToList();

            // when
            IReadOnlyList<DismissableApprovalReview> actualDismissableApprovalReviews =
                await this.accessBroker.FindDismissableApprovalReviewsAsync(
                    approvalId: approvalId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualDismissableApprovalReviews.Should().BeEquivalentTo(
                expectedDismissableApprovalReviews);

            VerifyDismissableApprovalReviewsQueriedOnce();
        }

        private void SetupDismissableApprovalReviewsQueryOver(
            params ApprovalReview[] storageApprovalReviews) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewsAsync(
                    It.IsAny<Func<IQueryable<ApprovalReview>, IQueryable<DismissableApprovalReview>>>(),
                    TestContext.Current.CancellationToken))
                        .Returns((
                            Func<IQueryable<ApprovalReview>, IQueryable<DismissableApprovalReview>> query,
                            CancellationToken _) =>
                            new ValueTask<IReadOnlyList<DismissableApprovalReview>>(
                                query(storageApprovalReviews.AsQueryable()).ToList()));

        private void VerifyDismissableApprovalReviewsQueriedOnce() =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewsAsync(
                    It.IsAny<Func<IQueryable<ApprovalReview>, IQueryable<DismissableApprovalReview>>>(),
                    TestContext.Current.CancellationToken),
                Times.Once);

        private static ApprovalReview CreateDismissableApprovalReview(
            Guid approvalId,
            ApprovalStatus statusId,
            DateTimeOffset createdWhen,
            bool isDeleted = false) =>
            new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                CreatedBy = "reviewer-" + GetRandomString(),
                CreatedWhen = createdWhen,
                StatusId = statusId,
                IsDeleted = isDeleted,
            };
    }
}
