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

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalSettingReviewerRole> InsertApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingReviewerRole>> SelectAllApprovalSettingReviewerRolesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> SelectApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> UpdateApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> DeleteApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalSettingReviewerRole>> BulkReadApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalSettingReviewerRolesAsync(
            List<ApprovalSettingReviewerRole> approvalSettingReviewerRoles,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalSettingReviewerRoleAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default);
    }
}
