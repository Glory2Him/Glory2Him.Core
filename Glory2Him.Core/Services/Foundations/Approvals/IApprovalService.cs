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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    internal partial interface IApprovalService
    {
        ValueTask<Approval> AddApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Approval>> RetrieveAllApprovalsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Approval> RetrieveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        ValueTask<Approval> ModifyApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default);

        ValueTask<Approval> RemoveApprovalByIdAsync(
            Guid approvalId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Approval> HardRemoveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up the approval occupying <c>(EntityType, EntityId)</c> — the pair
        /// <c>UX_Approvals_EntityType_EntityId</c> keys on — over the UNFILTERED store,
        /// spanning soft-deleted rows (design §9.7.2 rule 3).
        ///
        /// <para>The retrieve-or-create flow needs this because that index is <b>not</b>
        /// filtered on <c>IsDeleted</c>, so a closed approval still occupies the key. The
        /// caller-facing reads are visibility-filtered and would answer "does not exist" for a
        /// key that does — and the insert that answer invites can never succeed. The flow
        /// reinstates the row in place instead (§12.4.4 BR14).</para>
        ///
        /// <para>Returns a non-leaking <see cref="ApprovalEntityMatch"/> projection — id,
        /// status and soft-delete flag only — or <c>null</c> when the pair is unoccupied. The
        /// row body never crosses back.</para>
        /// </summary>
        ValueTask<ApprovalEntityMatch?> FindApprovalByEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);
    }
}
