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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Arrangement for the approve endpoints, shared by every approvable entity.
    ///
    /// <para>The approve decision reads the APPROVAL row's status, not the entity's, and no
    /// endpoint in this host creates that row — submitting an entity writes only its own
    /// <c>ApprovalStatus</c>, and the round would normally be opened by the approval
    /// orchestration reacting to the published fact. So the round has to be arranged beneath
    /// HTTP. These are real rows written through the host's own storage broker, read back by the
    /// production <c>AccessBroker</c> through the production <c>StorageBroker</c>.</para>
    ///
    /// <para>Entity ROWS are arranged per entity, in <c>ApiBroker.&lt;Entity&gt;Arrangements.cs</c>.
    /// What lives here is only what every approvable entity shares.</para>
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>
        /// Opens a submitted approval round against any entity.
        ///
        /// <para>The <paramref name="entityType"/> is a PARAMETER rather than a constant, and
        /// that is load-bearing: the approve decision resolves the entity behind the approval
        /// and composes the reviewer's expected role from its type, so a round arranged under
        /// the wrong type is decided against the wrong role and the test passes or fails for a
        /// reason it never meant to express. This arrangement was written for Tag and hard-coded
        /// it; the second exposer to need it is what turned the constant into an argument.</para>
        /// </summary>
        public async ValueTask<Approval> InsertSubmittedApprovalAsync(
            EntityType entityType,
            Guid entityId,
            string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = ApprovalStatus.Submitted,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalAsync(approval);
        }

        /// <summary>
        /// The approving caller must hold no active review of their own — a publisher who filed
        /// a review has spent their vote on the round — so the reviewer here is a third party.
        /// </summary>
        public async ValueTask<ApprovalReview> InsertApprovedReviewAsync(
            Guid approvalId,
            string reviewerUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReview = new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = ApprovalStatus.Approved,
                Comment = "Arranged by the acceptance suite.",
                IsDeleted = false,
                CreatedBy = reviewerUserId,
                CreatedWhen = now,
                UpdatedBy = reviewerUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalReviewAsync(approvalReview);
        }

        public async ValueTask RemoveApprovalReviewAsync(ApprovalReview approvalReview) =>
            await this.storageBroker.DeleteApprovalReviewAsync(approvalReview);

        public async ValueTask RemoveApprovalAsync(Approval approval) =>
            await this.storageBroker.DeleteApprovalAsync(approval);

    }
}
