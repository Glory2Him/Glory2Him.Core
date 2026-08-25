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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItemSettings
{
    /// <summary>
    /// §14.7 posture C, and this suite is written rather than copied because posture C differs
    /// from the content exposers' posture A in both directions.
    ///
    /// <para><b>Writes are narrower.</b> Every write including hard removal is <c>Admin</c> only.
    /// There is no owner branch and no scoped review tier — <c>Roles.cs</c> declares no
    /// <c>ContentItemSetting-*</c> constants at all — so the contributor, reviewer and
    /// owner-versus-moderator cases the posture A suites spend most of their length on do not
    /// exist here.</para>
    ///
    /// <para><b>Reads are PUBLIC, and that is the half that differs from
    /// <c>ApprovalSettingsController</c>.</b> The two share posture C and take opposite read
    /// gates: effective settings drive rendering for anonymous visitors, so this entity is
    /// public-read while approval policy is authenticated-read. Both directions are asserted
    /// below, because a gate flipped the wrong way either leaks policy or renders every anonymous
    /// page without its settings, and neither failure is loud.</para>
    /// </summary>
    public partial class ContentItemSettingApiTests
    {
        /// <summary>
        /// The permissive direction, and the reason this exposer cannot copy
        /// <c>ApprovalSettingsController</c>'s read gate: an anonymous visitor's page needs these
        /// rows to know whether to render its tags, reactions and comments. A 401 here would leak
        /// nothing — it would silently strip every public page of its settings.
        /// </summary>
        [Fact]
        public async Task ShouldAllowAnonymousReadOfCollectionAsync()
        {
            // given
            ContentItemSetting existingContentItemSetting = await PostRandomContentItemSettingAsync();

            try
            {
                this.apiBroker.ActAsAnonymous();

                // when
                List<ContentItemSetting> actualContentItemSettings =
                    await this.apiBroker.GetAllContentItemSettingsAsync();

                // then
                actualContentItemSettings.Should().Contain(contentItemSetting =>
                    contentItemSetting.Id == existingContentItemSetting.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        /// <summary>
        /// The authenticated read still works. Asserted alongside the anonymous one so that a
        /// change gating these on <c>Admin</c> — the natural mistake, since every WRITE here is
        /// <c>Admin</c> — fails on both.
        /// </summary>
        [Fact]
        public async Task ShouldAllowAnyAuthenticatedCallerToReadAsync()
        {
            // given
            ContentItemSetting existingContentItemSetting = await PostRandomContentItemSettingAsync();

            try
            {
                this.apiBroker.ActAsContributor();

                // when
                ContentItemSetting actualContentItemSetting =
                    await this.apiBroker.GetContentItemSettingByIdAsync(existingContentItemSetting.Id);

                // then
                actualContentItemSetting.Id.Should().Be(existingContentItemSetting.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Fact]
        public async Task ShouldRefuseAnonymousPostAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostContentItemSettingAsync(randomContentItemSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
            }
        }

        /// <summary>
        /// The whole of posture C rule 1 in one theory: only administrators author configuration.
        /// A <c>Publisher</c> is the interesting negative — they carry the highest tier this
        /// exposer's sibling controllers admit on their approve route, and it buys them nothing
        /// here.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefusePostIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostContentItemSettingAsync(randomContentItemSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => postTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
            }
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefusePutIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ContentItemSetting existingContentItemSetting = await PostRandomContentItemSettingAsync();

            ContentItemSetting modifiedContentItemSetting =
                UpdateContentItemSettingWithRandomValues(existingContentItemSetting);

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var putTask =
                    this.apiBroker.PutContentItemSettingAsync(modifiedContentItemSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => putTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefuseDeleteIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ContentItemSetting existingContentItemSetting = await PostRandomContentItemSettingAsync();

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var deleteTask = this.apiBroker
                    .DeleteContentItemSettingByIdAsync(existingContentItemSetting.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => deleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefuseHardDeleteIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ContentItemSetting existingContentItemSetting = await PostRandomContentItemSettingAsync();

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var hardDeleteTask = this.apiBroker
                    .HardDeleteContentItemSettingByIdAsync(existingContentItemSetting.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfContentItemSettingDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getTask = this.apiBroker.GetContentItemSettingByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getTask);
        }

        /// <summary>
        /// §6.10 resolution depends on there being at most one setting per scope, and
        /// <c>UX_ContentItemSettings_DefaultPerType</c> is what makes that true rather than
        /// hoped-for (§12.5.2 business rule 3). A second default for the same content type is the
        /// ordinary way a caller reaches this.
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfContentTypeAlreadyHasADefaultAsync()
        {
            // given
            ContentItemSetting existingContentItemSetting =
                await PostRandomContentItemSettingAsync();

            ContentItemSetting duplicateContentItemSetting = CreateRandomContentItemSetting();
            duplicateContentItemSetting.ContentType = existingContentItemSetting.ContentType;
            duplicateContentItemSetting.ContentItemId = null;

            try
            {
                // when
                var postTask = this.apiBroker
                    .PostContentItemSettingAsync(duplicateContentItemSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    duplicateContentItemSetting.Id);
            }
        }

        /// <summary>
        /// The second index (§12.5.2 business rule 4), and a genuinely different rule rather than
        /// the same one restated: a content type may carry one default AND one override per item,
        /// so an override must be permitted alongside a default while a second override for the
        /// same item must not be.
        ///
        /// <para><c>ContentItemId</c> carries no foreign key, so the item id here need not name a
        /// real content item. That is the schema's choice rather than this test's convenience —
        /// worth knowing, because it means a typo in an override's target is stored happily and
        /// simply never resolves.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfContentItemAlreadyHasAnOverrideAsync()
        {
            // given
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting overrideContentItemSetting = CreateRandomContentItemSetting();
            overrideContentItemSetting.ContentItemId = contentItemId;

            ContentItemSetting createdOverride = await this.apiBroker
                .PostContentItemSettingAsync(overrideContentItemSetting);

            ContentItemSetting duplicateContentItemSetting = CreateRandomContentItemSetting();
            duplicateContentItemSetting.ContentItemId = contentItemId;

            try
            {
                // when
                var postTask = this.apiBroker
                    .PostContentItemSettingAsync(duplicateContentItemSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(createdOverride.Id);

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    duplicateContentItemSetting.Id);
            }
        }
    }
}
