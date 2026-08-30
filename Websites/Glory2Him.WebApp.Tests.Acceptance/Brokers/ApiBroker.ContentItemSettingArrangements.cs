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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Microsoft.EntityFrameworkCore;
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// ContentItemSetting rows torn down beneath HTTP — the sibling of
    /// <c>ApiBroker.TagArrangements.cs</c> and its counterparts.
    ///
    /// <para><b>Teardown here is physical, and the reason has changed.</b> It was once load-bearing
    /// for uniqueness: neither <c>UX_ContentItemSettings_DefaultPerType</c> nor
    /// <c>UX_ContentItemSettings_OverridePerEntity</c> carried an <c>IsDeleted</c> term, so a
    /// soft-deleted row still occupied its scope and a suite tearing down through the API's own
    /// delete would have left every scope it touched permanently taken. #326 added the term to
    /// both, so a soft delete now genuinely releases a scope. The physical removal stays for the
    /// ordinary reason every suite has one — the row itself must not outlive the test, or the
    /// collection reads see it.</para>
    ///
    /// <para>The insert arrangement below exists for the default tier alone, and not to arrange
    /// ordinary rows: the host seeds one default per content type at startup, so a test that needs
    /// a default slot free has to take the seeded incumbent out and put it back exactly as it was
    /// — and since #387 the API refuses to remove a default at all, so a soft-deleted one can only
    /// be arranged here. Everything else this suite needs is created through the endpoint under
    /// test — this exposer has no approval round to open, <c>ContentItemSetting</c> carrying no
    /// <c>ApprovalStatus</c> at all.</para>
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

        /// <summary>
        /// The LIVE per-type default, whatever its id — <c>ContentItemSettingSeedData</c> mints a
        /// fresh <c>Guid</c> per environment, so it can only be found by its scope.
        ///
        /// <para>The <c>IsDeleted</c> term is not decoration. <c>UX_ContentItemSettings_DefaultPerType</c>
        /// now constrains live rows only (#326), so a scope may legitimately hold one live default
        /// alongside soft-deleted predecessors and the scope alone no longer names a single row.
        /// Without the term this would return whichever the query happened to reach first, and a
        /// caller lifting a default out of its slot could remove a predecessor while the live row
        /// went on occupying it.</para>
        /// </summary>
        public async ValueTask<CoreContentItemSetting> GetCoreDefaultContentItemSettingAsync(
            ContentType contentType)
        {
            IQueryable<CoreContentItemSetting> allContentItemSettings =
                await this.storageBroker.SelectAllContentItemSettingsAsync();

            return await allContentItemSettings.FirstOrDefaultAsync(
                contentItemSetting =>
                    contentItemSetting.ContentType == contentType
                    && contentItemSetting.ContentItemId == null
                    && contentItemSetting.IsDeleted == false);
        }

        /// <summary>
        /// Writes a row beneath HTTP, audit fields and id included. Two callers, and they are
        /// opposite halves of the same test: it puts the seeded default back exactly as it was
        /// after freeing its slot, and it arranges the soft-deleted predecessor that the delete
        /// endpoint will no longer produce — a default may not be removed at all (#387).
        ///
        /// <para>The restore is not redundant against the seed. <c>ContentItemSettingSeedData</c>
        /// does now replace a missing LIVE default, but only at startup, and nothing restarts
        /// mid-suite — the tests that follow depend on this call, not on the seed.</para>
        /// </summary>
        public async ValueTask<CoreContentItemSetting> InsertCoreContentItemSettingAsync(
            CoreContentItemSetting contentItemSetting) =>
            await this.storageBroker.InsertContentItemSettingAsync(contentItemSetting);
    }
}
