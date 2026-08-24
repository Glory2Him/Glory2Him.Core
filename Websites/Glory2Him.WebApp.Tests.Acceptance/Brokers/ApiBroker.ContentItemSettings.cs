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
using System.Threading.Tasks;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string contentItemSettingsRelativeUrl = "api/contentItemSettings";

        public async ValueTask<ContentItemSetting> PostContentItemSettingAsync(ContentItemSetting contentItemSetting) =>
            await this.apiFactoryClient.PostContentAsync(contentItemSettingsRelativeUrl, contentItemSetting);

        public async ValueTask<List<ContentItemSetting>> GetAllContentItemSettingsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItemSetting>>($"{contentItemSettingsRelativeUrl}/");

        public async ValueTask<List<ContentItemSetting>> GetSpecificContentItemSettingByIdAsync(Guid contentItemSettingId) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItemSetting>>(
                $"{contentItemSettingsRelativeUrl}?$filter=Id eq {contentItemSettingId}");

        public async ValueTask<ContentItemSetting> GetContentItemSettingByIdAsync(Guid contentItemSettingId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItemSetting>($"{contentItemSettingsRelativeUrl}/{contentItemSettingId}");

        public async ValueTask<ContentItemSetting> DeleteContentItemSettingByIdAsync(Guid contentItemSettingId) =>
            await this.apiFactoryClient.DeleteContentAsync<ContentItemSetting>($"{contentItemSettingsRelativeUrl}/{contentItemSettingId}");

        public async ValueTask<ContentItemSetting> HardDeleteContentItemSettingByIdAsync(Guid contentItemSettingId) =>
            await this.apiFactoryClient.DeleteContentAsync<ContentItemSetting>($"{contentItemSettingsRelativeUrl}/{contentItemSettingId}/hard");



        public async ValueTask<ContentItemSetting> PutContentItemSettingAsync(ContentItemSetting contentItemSetting) =>
            await this.apiFactoryClient.PutContentAsync(contentItemSettingsRelativeUrl, contentItemSetting);
    }
}
