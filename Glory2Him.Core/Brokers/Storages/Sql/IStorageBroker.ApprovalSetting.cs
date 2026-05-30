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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ApprovalSetting> InsertApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalSetting>> SelectAllApprovalSettingsAsync();

        ValueTask<ApprovalSetting> SelectApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSetting> UpdateApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalSetting> DeleteApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ApprovalSetting>> BulkReadApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsApprovalSettingAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default);
    }
}
