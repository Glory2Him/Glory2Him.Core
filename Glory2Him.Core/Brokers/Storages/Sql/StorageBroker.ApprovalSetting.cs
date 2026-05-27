// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalSetting> ApprovalSettings { get; set; }

        public async ValueTask<ApprovalSetting> InsertApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalSetting, cancellationToken);

        public async ValueTask<IQueryable<ApprovalSetting>> SelectAllApprovalSettingsAsync() =>
            await SelectAllAsync<ApprovalSetting>();

        public async ValueTask<ApprovalSetting> SelectApprovalSettingByIdAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalSetting>(new object[] { approvalSettingId }, cancellationToken);

        public async ValueTask<ApprovalSetting> UpdateApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalSetting, cancellationToken);

        public async ValueTask<ApprovalSetting> DeleteApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalSetting, cancellationToken);

        public async ValueTask BulkInsertApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalSettings, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalSettings, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalSettings, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalSetting>> BulkReadApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalSettings, cancellationToken);

        public async ValueTask BulkUpsertApprovalSettingsAsync(
            List<ApprovalSetting> approvalSettings,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalSettings, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalSettingAsync(
            Guid approvalSettingId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalSetting>(new object[] { approvalSettingId }, cancellationToken);
    }
}
