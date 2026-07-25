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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial interface IApprovalSettingPublisherRoleService
    {
        ValueTask<ApprovalSettingPublisherRole> AddApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingPublisherRole>> RetrieveAllApprovalSettingPublisherRolesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> RetrieveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> ModifyApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> RemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingPublisherRole> HardRemoveApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default);
    }
}
