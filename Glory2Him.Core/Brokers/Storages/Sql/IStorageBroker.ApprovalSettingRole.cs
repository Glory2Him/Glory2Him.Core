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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalSettingRole> InsertApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingRole>> SelectAllApprovalSettingRolesAsync();

        ValueTask<ApprovalSettingRole> SelectApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> UpdateApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> DeleteApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalSettingRole>> BulkReadApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalSettingRolesAsync(
            List<ApprovalSettingRole> approvalSettingRoles,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalSettingRoleAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default);
    }
}
