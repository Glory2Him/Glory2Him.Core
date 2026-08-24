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
using System.Threading.Tasks;
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// ContentItemSetting rows torn down beneath HTTP — the sibling of
    /// <c>ApiBroker.TagArrangements.cs</c> and its counterparts.
    ///
    /// <para><b>Teardown here matters more than it does for the content entities.</b> Neither
    /// <c>UX_ContentItemSettings_EntityTypeDefault</c> nor
    /// <c>UX_ContentItemSettings_EntityTypeContentType</c> carries an <c>IsDeleted</c> term, so a
    /// soft-deleted row still occupies its scope. A test that tore down through the API's own
    /// delete would leave that entity type's default slot permanently taken and every later test
    /// writing to it would get a 409 out of nowhere. The physical removal below is what keeps the
    /// suite's scopes reusable.</para>
    ///
    /// <para>There is deliberately no insert arrangement. This exposer has no approval round to
    /// open — <c>ContentItemSetting</c> carries no <c>ApprovalStatus</c> at all — so every row this
    /// suite needs can be created through the endpoint under test.</para>
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreContentItemSetting> GetCoreContentItemSettingByIdAsync(
            Guid contentItemSettingId) =>
            await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId);

        public async ValueTask RemoveCoreContentItemSettingByIdAsync(Guid contentItemSettingId)
        {
            CoreContentItemSetting storedContentItemSetting =
                await this.storageBroker.SelectContentItemSettingByIdAsync(contentItemSettingId);

            if (storedContentItemSetting is not null)
            {
                await this.storageBroker.DeleteContentItemSettingAsync(storedContentItemSetting);
            }
        }
    }
}
