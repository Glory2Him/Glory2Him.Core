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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ContentItemSetting> ContentItemSettings { get; set; }

        public async ValueTask<ContentItemSetting> InsertContentItemSettingAsync(
            ContentItemSetting contentItemSetting, CancellationToken cancellationToken = default) =>
                await InsertAsync(contentItemSetting, cancellationToken);

        public async ValueTask<IQueryable<ContentItemSetting>> SelectAllContentItemSettingsAsync() =>
            await SelectAllAsync<ContentItemSetting>();

        public async ValueTask<ContentItemSetting> SelectContentItemSettingByIdAsync(
            Guid contentItemSettingId, CancellationToken cancellationToken = default) =>
                await SelectAsync<ContentItemSetting>(new object[] { contentItemSettingId }, cancellationToken);

        public async ValueTask<ContentItemSetting> UpdateContentItemSettingAsync(
            ContentItemSetting contentItemSetting, CancellationToken cancellationToken = default) =>
                await UpdateAsync(contentItemSetting, cancellationToken);

        public async ValueTask<ContentItemSetting> DeleteContentItemSettingAsync(
            ContentItemSetting contentItemSetting, CancellationToken cancellationToken = default) =>
                await DeleteAsync(contentItemSetting, cancellationToken);

        public async ValueTask BulkInsertContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings, CancellationToken cancellationToken = default) =>
                await BulkInsertAsync(contentItemSettings, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings, CancellationToken cancellationToken = default) =>
                await BulkUpdateAsync(contentItemSettings, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings, CancellationToken cancellationToken = default) =>
                await BulkDeleteAsync(contentItemSettings, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ContentItemSetting>> BulkReadContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(contentItemSettings, cancellationToken);

        public async ValueTask BulkUpsertContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(contentItemSettings, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsContentItemSettingAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ContentItemSetting>(new object[] { contentItemSettingId }, cancellationToken);
    }
}