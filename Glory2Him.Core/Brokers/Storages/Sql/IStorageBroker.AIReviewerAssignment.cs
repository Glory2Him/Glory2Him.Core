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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<AIReviewerAssignment> InsertAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> SelectAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The LIVE assignment on ONE approval round, or <c>null</c> when Berean has never been
        /// assigned to it (or every prior assignment has since been removed).
        ///
        /// <para>Filtered to <c>IsDeleted == false</c> IN the query, unlike the broader-reach
        /// reads its sibling foundations leave unfiltered for the service to filter over. There
        /// is no visibility posture to re-apply here — the read gate this service applies runs
        /// against the SECURITY CONTEXT, not the row — and the filtered-unique-index invariant
        /// (design intent: at most one live row per <c>ApprovalId</c>) makes this deterministic:
        /// there is at most one row this query could ever return.</para>
        /// </summary>
        ValueTask<AIReviewerAssignment?> SelectAIReviewerAssignmentByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> UpdateAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> DeleteAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<AIReviewerAssignment>> BulkReadAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAIReviewerAssignmentAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default);
    }
}
