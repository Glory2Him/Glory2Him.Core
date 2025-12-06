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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentItemSetting> InsertContentItemSettingAsync(
        ContentItemSetting contentItemSetting);

        ValueTask<IQueryable<ContentItemSetting>> SelectAllContentItemSettingsAsync();
        ValueTask<ContentItemSetting> SelectContentItemSettingByIdAsync(Guid contentItemSettingId);

        ValueTask<ContentItemSetting> UpdateContentItemSettingAsync(
            ContentItemSetting contentItemSetting);

        ValueTask<ContentItemSetting> DeleteContentItemSettingAsync(
            ContentItemSetting contentItemSetting);

        ValueTask BulkInsertContentItemSettingsAsync(List<ContentItemSetting> contentItemSettings);
        ValueTask BulkUpdateContentItemSettingsAsync(List<ContentItemSetting> contentItemSettings);
        ValueTask BulkDeleteContentItemSettingsAsync(List<ContentItemSetting> contentItemSettings);
    }
}
