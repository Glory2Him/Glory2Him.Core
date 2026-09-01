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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        private void SetupApprovalReviewRequests(
            params ApprovalReviewRequest[] approvalReviewRequests) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApprovalReviewRequest>(
                        approvalReviewRequests).AsQueryable());

        /// <summary>
        /// The scope reports its reviewers TWICE, filtered and unfiltered, and the two sets must
        /// come apart. Invitability (§7.9 rules 4 and 5) turns on a review that still stands, so a
        /// dismissed or withdrawn one releases the person; who the round INVOLVED is released by
        /// nothing, and it is what the §16.7.4 name resolver's legal set is built from.
        ///
        /// <para>Only settled here. The orchestration's tests mock <c>IAccessBroker</c>, so they
        /// can assert what the resolver does with the two sets but never that the broker fills
        /// them differently — collapse them into one and the whole suite above stays green while
        /// the panel loses the name of every dismissed reviewer it renders.</para>
        /// </summary>
        [Fact]
        public async Task ShouldSeparateTheRecordedReviewersFromTheActiveOnesAsync()
        {
            // given: one row per way of failing the ACTIVE filter, so a set that drops any clause
            // is distinguishable from one that does not
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: EntityType.ContentItem,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-owner");

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "standing-reviewer",
                    statusId: ApprovalStatus.Approved),

                // Dismissed by a later edit (§9.5). Their verdict no longer counts, so they are
                // invitable again — but the panel still renders the row, so the resolver still
                // has to be able to name them.
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "dismissed-reviewer",
                    statusId: ApprovalStatus.Dismissed),

                // Withdrawn. Same split, reached the other way.
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "withdrawn-reviewer",
                    statusId: ApprovalStatus.Approved,
                    isDeleted: true),

                // ANOTHER round. Neither set may carry them, or one approval's panel would name
                // the people on every other approval in the table.
                CreateApprovalReview(
                    approvalId: otherApprovalId,
                    createdBy: "other-rounds-reviewer",
                    statusId: ApprovalStatus.Approved));

            SetupApprovalComments();
            SetupApprovalReviewRequests();

            // when
            ApprovalReviewerScope actualScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualScope.ActiveReviewerUserIds.Should().BeEquivalentTo(
                new[] { "standing-reviewer" },
                because: "a dismissed or withdrawn verdict leaves its author invitable again");

            actualScope.RecordedReviewerUserIds.Should().BeEquivalentTo(
                new[] { "standing-reviewer", "dismissed-reviewer", "withdrawn-reviewer" },
                because: "the panel renders those rows, so the resolver has to name their "
                    + "authors whatever became of the verdicts");
        }
    }
}
