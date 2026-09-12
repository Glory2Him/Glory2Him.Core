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
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        /// <summary>
        /// The overload adds no capability — it is <c>FindApprovalAsync</c> followed by the same
        /// gather the by-id form performs. The two entry points differ in how the round is named
        /// and in nothing else, so this proves the scopes they answer are the same scope.
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveApprovalReviewerScopeByEntityEquivalentToTheByIdFormAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            EntityType entityType = EntityType.ContentItem;

            Approval approval = CreateApproval(
                approvalId: approvalId,
                entityType: entityType,
                entityId: entityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovals(approval);
            SetupApprovalById(approval);
            SetupEntityAuthor(entityType, entityId, createdBy: "the-entity-owner");

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "standing-reviewer",
                    statusId: ApprovalStatus.Approved));

            SetupApprovalComments();
            SetupApprovalReviewRequests();

            // when
            ApprovalReviewerScope actualScopeById =
                await this.accessBroker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            ApprovalReviewerScope actualScopeByEntity =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: default);

            // then
            actualScopeByEntity.Should().BeEquivalentTo(actualScopeById,
                because: "the two entry points differ in how the round is named and in nothing "
                    + "else");
        }

        /// <summary>
        /// An entity key that no approval carries answers <c>null</c> — not an empty scope and
        /// not an exception. The by-id form answers <c>null</c> for an absent approval and stays
        /// distinguishable from an empty one, so the by-entity form must too.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNullWhenNoApprovalCarriesTheEntityKeyAsync()
        {
            // given
            SetupApprovals();

            // when
            ApprovalReviewerScope? actualScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: EntityType.ContentItem,
                    entityId: Guid.NewGuid(),
                    cancellationToken: default);

            // then
            actualScope.Should().BeNull(
                because: "an entity key that no approval carries answers null rather than an "
                    + "empty scope");
        }
    }
}
