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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingRoles
{
    public partial interface IApprovalSettingRoleService
    {
        ValueTask<ApprovalSettingRole> AddApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingRole>> RetrieveAllApprovalSettingRolesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> RetrieveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> ModifyApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> RemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingRole> HardRemoveApprovalSettingRoleByIdAsync(
            Guid approvalSettingRoleId,
            CancellationToken cancellationToken = default);
    }
}
