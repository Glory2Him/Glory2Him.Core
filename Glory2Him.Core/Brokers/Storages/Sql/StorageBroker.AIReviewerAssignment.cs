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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<AIReviewerAssignment> AIReviewerAssignments { get; set; }

        public async ValueTask<AIReviewerAssignment> InsertAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(aiReviewerAssignment, cancellationToken);

        public async ValueTask<AIReviewerAssignment> SelectAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<AIReviewerAssignment>(
                new object[] { aiReviewerAssignmentId }, cancellationToken);

        // ToListAsync/FirstOrDefaultAsync, not their synchronous twins: this is the only layer
        // that may name EF, and it is the layer that can actually give the token to the query.
        public async ValueTask<AIReviewerAssignment?> SelectAIReviewerAssignmentByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            await AIReviewerAssignments
                .Where(aiReviewerAssignment =>
                    aiReviewerAssignment.ApprovalId == approvalId
                        && aiReviewerAssignment.IsDeleted == false)
                .FirstOrDefaultAsync(cancellationToken);

        public async ValueTask<AIReviewerAssignment> UpdateAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(aiReviewerAssignment, cancellationToken);

        public async ValueTask<AIReviewerAssignment> DeleteAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(aiReviewerAssignment, cancellationToken);

        public async ValueTask BulkInsertAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(aiReviewerAssignments, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(aiReviewerAssignments, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(aiReviewerAssignments, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<AIReviewerAssignment>> BulkReadAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(aiReviewerAssignments, cancellationToken);

        public async ValueTask BulkUpsertAIReviewerAssignmentsAsync(
            List<AIReviewerAssignment> aiReviewerAssignments,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(aiReviewerAssignments, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsAIReviewerAssignmentAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<AIReviewerAssignment>(
                new object[] { aiReviewerAssignmentId }, cancellationToken);
    }
}
