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
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string approvalSettingsRelativeUrl = "api/approvalSettings";

        public async ValueTask<ApprovalSetting> PostApprovalSettingAsync(ApprovalSetting approvalSetting) =>
            await this.apiFactoryClient.PostContentAsync(approvalSettingsRelativeUrl, approvalSetting);

        public async ValueTask<List<ApprovalSetting>> GetAllApprovalSettingsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalSetting>>($"{approvalSettingsRelativeUrl}/");

        public async ValueTask<List<ApprovalSetting>> GetSpecificApprovalSettingByIdAsync(Guid approvalSettingId) =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalSetting>>(
                $"{approvalSettingsRelativeUrl}?$filter=Id eq {approvalSettingId}");

        public async ValueTask<ApprovalSetting> GetApprovalSettingByIdAsync(Guid approvalSettingId) =>
            await this.apiFactoryClient.GetContentAsync<ApprovalSetting>($"{approvalSettingsRelativeUrl}/{approvalSettingId}");

        public async ValueTask<ApprovalSetting> DeleteApprovalSettingByIdAsync(Guid approvalSettingId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalSetting>($"{approvalSettingsRelativeUrl}/{approvalSettingId}");

        public async ValueTask<ApprovalSetting> HardDeleteApprovalSettingByIdAsync(Guid approvalSettingId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalSetting>($"{approvalSettingsRelativeUrl}/{approvalSettingId}/hard");



        public async ValueTask<ApprovalSetting> PutApprovalSettingAsync(ApprovalSetting approvalSetting) =>
            await this.apiFactoryClient.PutContentAsync(approvalSettingsRelativeUrl, approvalSetting);
    }
}
