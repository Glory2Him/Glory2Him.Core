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
using Glory2Him.Core.Models.Foundations.Approvals;
using CoreApprovalComment = Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Arrangement for the approval-comment endpoints. Every comment hangs off an
    /// <c>Approval</c> — a real foreign key, and a row no endpoint in this host creates, since
    /// opening a round is the approval orchestration's job. The round must also be
    /// <c>Submitted</c> and not taken down, because that is exactly the pair of facts
    /// <c>AccessBroker</c> reads before it will let a comment be written (§7.7 rule 1).
    ///
    /// <para>So the parent is arranged beneath HTTP, through the host's own storage broker, and
    /// read back by the production access and storage brokers on the way through. Teardown goes
    /// the same way: the API's own delete is a SOFT delete, so tearing down through the endpoint
    /// would leave the row behind.</para>
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>
        /// An approval round that is open for comment. <c>EntityId</c> is not a foreign key —
        /// only the (EntityType, EntityId) pair is unique — so a fresh id needs no target row,
        /// and these tests are about the comment thread rather than what is under review.
        /// </summary>
        public async ValueTask<Approval> InsertOpenApprovalAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Tag,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Submitted,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalAsync(approval);
        }

        public async ValueTask<CoreApprovalComment> GetCoreApprovalCommentByIdAsync(Guid approvalCommentId) =>
            await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId);

        /// <summary>
        /// Physically removes a comment if it is still there, whatever state it is in — the
        /// counterpart of <c>RemoveCoreTagByIdAsync</c>, and for the same reasons: a test that
        /// tore down through the endpoint left a soft-deleted row behind, and one whose assertion
        /// threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreApprovalCommentByIdAsync(Guid approvalCommentId)
        {
            CoreApprovalComment storedApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId);

            if (storedApprovalComment is not null)
            {
                await this.storageBroker.DeleteApprovalCommentAsync(storedApprovalComment);
            }
        }

        /// <summary>
        /// Removes the parent round. The comments hanging off it are removed first — the foreign
        /// key is <c>NoAction</c>, so an orphaned child would block this.
        /// </summary>
        public async ValueTask RemoveApprovalByIdAsync(Guid approvalId)
        {
            Approval storedApproval = await this.storageBroker.SelectApprovalByIdAsync(approvalId);

            if (storedApproval is not null)
            {
                await this.storageBroker.DeleteApprovalAsync(storedApproval);
            }
        }
    }
}
