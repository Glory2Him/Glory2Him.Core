// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalSettingRole> ApprovalSettingRoles { get; set; }

        public async ValueTask<ApprovalSettingRole> InsertApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalSettingRole, cancellationToken);

        public async ValueTask<IQueryable<ApprovalSettingRole>> SelectAllApprovalSettingRolesAsync() =>
            await SelectAllAsync<ApprovalSettingRole>();

        public async ValueTask<ApprovalSettingRole> SelectApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalSettingRole>(new object[] { approvalSettingRoleId }, cancellationToken);

        public async ValueTask<ApprovalSettingRole> UpdateApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalSettingRole, cancellationToken);

        public async ValueTask<ApprovalSettingRole> DeleteApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalSettingRole, cancellationToken);

        public async ValueTask BulkInsertApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalSettingRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalSettingRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalSettingRoles, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalSettingRole>> BulkReadApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalSettingRoles, cancellationToken);

        public async ValueTask BulkUpsertApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalSettingRoles, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalSettingRoleAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalSettingRole>(new object[] { approvalSettingRoleId }, cancellationToken);
    }
}
