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
using System.Threading.Tasks;
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ContentItemSetting> ContentItemSettings { get; set; }

        public async ValueTask<ContentItemSetting> InsertContentItemSettingAsync(
            ContentItemSetting contentItemSetting) =>
                await InsertAsync(contentItemSetting);

        public async ValueTask<IQueryable<ContentItemSetting>> SelectAllContentItemSettingsAsync() =>
            await SelectAllAsync<ContentItemSetting>();

        public async ValueTask<ContentItemSetting> SelectContentItemSettingByIdAsync(
            Guid contentItemSettingId) =>
                await SelectAsync<ContentItemSetting>(contentItemSettingId);

        public async ValueTask<ContentItemSetting> UpdateContentItemSettingAsync(
            ContentItemSetting contentItemSetting) =>
                await UpdateAsync(contentItemSetting);

        public async ValueTask<ContentItemSetting> DeleteContentItemSettingAsync(
            ContentItemSetting contentItemSetting) =>
                await DeleteAsync(contentItemSetting);

        public async ValueTask BulkInsertContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings) =>
                await BulkInsertAsync(contentItemSettings);

        public async ValueTask BulkUpdateContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings) =>
                await BulkUpdateAsync(contentItemSettings);

        public async ValueTask BulkDeleteContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings) =>
                await BulkDeleteAsync(contentItemSettings);
    }
}