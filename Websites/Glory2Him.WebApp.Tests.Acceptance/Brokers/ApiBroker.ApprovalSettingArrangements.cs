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
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// ApprovalSetting rows torn down beneath HTTP — the sibling of
    /// <c>ApiBroker.TagArrangements.cs</c> and its counterparts.
    ///
    /// <para><b>Teardown here is physical, and the reason has changed.</b> It was once load-bearing
    /// for uniqueness: neither <c>UX_ApprovalSettings_EntityTypeDefault</c> nor
    /// <c>UX_ApprovalSettings_EntityTypeContentType</c> carried an <c>IsDeleted</c> term, so a
    /// soft-deleted row still occupied its scope and a suite tearing down through the API's own
    /// delete would have left that entity type's default slot permanently taken — every later
    /// test writing to it getting a 409 out of nowhere. #326 added the term to both, so a soft
    /// delete now genuinely releases a scope. The physical removal stays for the ordinary reason
    /// every suite has one — the row itself must not outlive the test, or the collection reads
    /// see it. With only eight <c>EntityType</c> members to hand round, it also keeps the supply
    /// of free scopes honest.</para>
    ///
    /// <para>The insert arrangement below exists for the default tier alone, and not to arrange
    /// ordinary rows: <c>ApprovalSettingSeedData</c> seeds one default per entity type at
    /// startup, so a test that needs a default slot free has to take the seeded incumbent out
    /// and put it back exactly as it was. Everything else this suite needs is created through
    /// the endpoint under test — this exposer has no approval round to open,
    /// <c>ApprovalSetting</c> carrying no <c>ApprovalStatus</c> at all.</para>
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreApprovalSetting> GetCoreApprovalSettingByIdAsync(
            Guid approvalSettingId) =>
            await this.storageBroker.SelectApprovalSettingByIdAsync(approvalSettingId);

        public async ValueTask RemoveCoreApprovalSettingByIdAsync(Guid approvalSettingId)
        {
            CoreApprovalSetting storedApprovalSetting =
                await this.storageBroker.SelectApprovalSettingByIdAsync(approvalSettingId);

            if (storedApprovalSetting is not null)
            {
                await this.storageBroker.DeleteApprovalSettingAsync(storedApprovalSetting);
            }
        }

        /// <summary>
        /// The LIVE entity-type default, whatever its id — <c>ApprovalSettingSeedData</c> mints a
        /// fresh <c>Guid</c> per environment, so it can only be found by its scope.
        ///
        /// <para>The <c>IsDeleted</c> term is not decoration. <c>UX_ApprovalSettings_EntityTypeDefault</c>
        /// constrains live rows only (#326), so a scope may hold one live default alongside
        /// soft-deleted predecessors and the scope alone no longer names a single row.</para>
        /// </summary>
        public async ValueTask<CoreApprovalSetting> GetCoreDefaultApprovalSettingAsync(
            EntityType entityType)
        {
            IQueryable<CoreApprovalSetting> allApprovalSettings =
                await this.storageBroker.SelectAllApprovalSettingsAsync();

            return await allApprovalSettings.FirstOrDefaultAsync(
                approvalSetting =>
                    approvalSetting.EntityType == entityType
                    && approvalSetting.ContentType == null
                    && approvalSetting.IsDeleted == false);
        }

        /// <summary>
        /// Writes a row beneath HTTP, audit fields and id included. Two callers, and they are
        /// opposite halves of the same test: it puts the seeded default back exactly as it was
        /// after freeing its slot, and it arranges the soft-deleted predecessor in the tier the
        /// suite can no longer post to.
        /// </summary>
        public async ValueTask<CoreApprovalSetting> InsertCoreApprovalSettingAsync(
            CoreApprovalSetting approvalSetting) =>
            await this.storageBroker.InsertApprovalSettingAsync(approvalSetting);
    }
}
