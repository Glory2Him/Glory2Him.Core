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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<ApprovalSettingReviewerRole> ApprovalSettingReviewerRoles { get; set; }

        public async ValueTask<ApprovalSettingReviewerRole> InsertApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalSettingReviewerRole, cancellationToken);

        public async ValueTask<IQueryable<ApprovalSettingReviewerRole>> SelectAllApprovalSettingReviewerRolesAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<ApprovalSettingReviewerRole>(cancellationToken);

        public async ValueTask<ApprovalSettingReviewerRole> SelectApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalSettingReviewerRole>(new object[] { approvalSettingReviewerRoleId }, cancellationToken);

        public async ValueTask<ApprovalSettingReviewerRole> UpdateApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalSettingReviewerRole, cancellationToken);

        public async ValueTask<ApprovalSettingReviewerRole> DeleteApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalSettingReviewerRole, cancellationToken);

        public async ValueTask BulkInsertApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalSettingReviewerRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalSettingReviewerRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalSettingReviewerRoles, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalSettingReviewerRole>> BulkReadApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalSettingReviewerRoles, cancellationToken);

        public async ValueTask BulkUpsertApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalSettingReviewerRoles, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalSettingReviewerRoleAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalSettingReviewerRole>(new object[] { approvalSettingReviewerRoleId }, cancellationToken);
    }
}
