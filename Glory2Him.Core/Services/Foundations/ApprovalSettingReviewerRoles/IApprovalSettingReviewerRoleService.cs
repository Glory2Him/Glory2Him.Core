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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial interface IApprovalSettingReviewerRoleService
    {
        ValueTask<ApprovalSettingReviewerRole> AddApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSettingReviewerRole>> RetrieveAllApprovalSettingReviewerRolesAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> RetrieveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> ModifyApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> RemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSettingReviewerRole> HardRemoveApprovalSettingReviewerRoleByIdAsync(
            Guid approvalSettingReviewerRoleId,
            CancellationToken cancellationToken = default);
    }
}
