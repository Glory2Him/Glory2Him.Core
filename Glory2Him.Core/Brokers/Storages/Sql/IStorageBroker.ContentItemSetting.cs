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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial interface IStorageBroker
    {
        ValueTask<ContentItemSetting> InsertContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItemSetting>> SelectAllContentItemSettingsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemSetting> SelectContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemSetting> UpdateContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItemSetting> DeleteContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<ContentItemSetting>> BulkReadContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertContentItemSettingsAsync(
            List<ContentItemSetting> contentItemSettings,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsContentItemSettingAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default);
    }
}
