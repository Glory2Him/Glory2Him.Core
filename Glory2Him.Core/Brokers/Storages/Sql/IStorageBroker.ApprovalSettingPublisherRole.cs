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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<ApprovalSettingPublisherRole> InsertApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingPublisherRole>> SelectAllApprovalSettingPublisherRolesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> SelectApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> UpdateApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> DeleteApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalSettingPublisherRole>> BulkReadApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalSettingPublisherRoleAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default);
    }
}
