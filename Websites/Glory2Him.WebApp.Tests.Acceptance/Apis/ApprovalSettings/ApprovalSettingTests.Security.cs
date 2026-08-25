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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalSettings
{
    /// <summary>
    /// §14.7 posture C, and this suite is written rather than copied because posture C differs
    /// from the content exposers' posture A in both directions.
    ///
    /// <para><b>Writes are narrower.</b> Every write including hard removal is <c>Admin</c> only.
    /// There is no owner branch and no scoped review tier — <c>Roles.cs</c> declares no
    /// <c>ApprovalSetting-*</c> constants at all — so the contributor, reviewer and
    /// owner-versus-moderator cases the posture A suites spend most of their length on do not
    /// exist here.</para>
    ///
    /// <para><b>Reads are narrower too.</b> Any authenticated caller may see the rules their
    /// submissions run under; an anonymous one gets nothing. There is no §14.1 public-visibility
    /// concept, so unlike a tag or a reaction there is no publicly readable row at all.</para>
    /// </summary>
    public partial class ApprovalSettingApiTests
    {
        [Fact]
        public async Task ShouldRefuseAnonymousReadOfCollectionAsync()
        {
            // given
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();

            try
            {
                this.apiBroker.ActAsAnonymous();

                // when
                var getAllTask = this.apiBroker.GetAllApprovalSettingsAsync().AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getAllTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);
            }
        }

        /// <summary>
        /// The half of posture C rule 2 that is easy to lose. It would be tempting to gate these
        /// reads on <c>Admin</c> like the writes; the design says otherwise, because a contributor
        /// needs to see the policy their submission will be judged under.
        /// </summary>
        [Fact]
        public async Task ShouldAllowAnyAuthenticatedCallerToReadAsync()
        {
            // given
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();

            try
            {
                this.apiBroker.ActAsContributor();

                // when
                ApprovalSetting actualApprovalSetting =
                    await this.apiBroker.GetApprovalSettingByIdAsync(existingApprovalSetting.Id);

                // then
                actualApprovalSetting.Id.Should().Be(existingApprovalSetting.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);
            }
        }

        [Fact]
        public async Task ShouldRefuseAnonymousPostAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(randomApprovalSetting).AsTask();

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
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(randomApprovalSetting).AsTask();

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
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();

            ApprovalSetting modifiedApprovalSetting =
                UpdateApprovalSettingWithRandomValues(existingApprovalSetting);

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var putTask =
                    this.apiBroker.PutApprovalSettingAsync(modifiedApprovalSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => putTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefuseDeleteIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var deleteTask = this.apiBroker
                    .DeleteApprovalSettingByIdAsync(existingApprovalSetting.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => deleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldRefuseHardDeleteIfCallerIsNotAdministratorAsync(string roleName)
        {
            // given
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();

            try
            {
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

                // when
                var hardDeleteTask = this.apiBroker
                    .HardDeleteApprovalSettingByIdAsync(existingApprovalSetting.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfApprovalSettingDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getTask = this.apiBroker.GetApprovalSettingByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getTask);
        }

        /// <summary>
        /// §8.4 policy resolution depends on there being at most one setting per scope, and
        /// <c>UX_ApprovalSettings_EntityTypeDefault</c> is what makes that true rather than
        /// hoped-for. A second default for the same entity type is the ordinary way a caller
        /// reaches this.
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfEntityTypeAlreadyHasADefaultAsync()
        {
            // given
            ApprovalSetting existingApprovalSetting = await PostRandomApprovalSettingAsync();
            ApprovalSetting duplicateApprovalSetting = CreateRandomApprovalSetting();
            duplicateApprovalSetting.EntityType = existingApprovalSetting.EntityType;
            duplicateApprovalSetting.ContentType = null;

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(duplicateApprovalSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    existingApprovalSetting.Id);

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    duplicateApprovalSetting.Id);
            }
        }

        /// <summary>
        /// The second index, and it is a genuinely different rule rather than the same one
        /// restated: an entity type may carry one default AND one override per content type, so
        /// the pair below must be permitted alongside the default the first arrangement creates,
        /// while a second row for the same pair must not be.
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfEntityTypeAndContentTypeAlreadyPairedAsync()
        {
            // given
            ApprovalSetting overrideApprovalSetting = CreateRandomApprovalSetting();

            // ContentItem, and not by preference: CK_ApprovalSetting_ContentTypeRequiresContentItem
            // permits a populated ContentType only on that entity type (design §8.4, §18.6 rule 5
            // — it is the one entity that carries a content type).
            overrideApprovalSetting.EntityType = EntityType.ContentItem;
            overrideApprovalSetting.ContentType = ContentType.Story;

            ApprovalSetting createdOverride =
                await this.apiBroker.PostApprovalSettingAsync(overrideApprovalSetting);

            ApprovalSetting duplicateApprovalSetting = CreateRandomApprovalSetting();
            duplicateApprovalSetting.EntityType = createdOverride.EntityType;
            duplicateApprovalSetting.ContentType = ContentType.Story;

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(duplicateApprovalSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(createdOverride.Id);

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    duplicateApprovalSetting.Id);
            }
        }

        /// <summary>
        /// <c>CK_ApprovalSetting_ContentTypeRequiresContentItem</c>, asserted from the outside.
        ///
        /// <para>Only <c>ContentItem</c> carries a content type (§18.6 rule 5), so a content-type
        /// scoped policy for any other entity type is meaningless — there is nothing for it to
        /// narrow. The rule is a database check constraint rather than a service validation,
        /// which is why it surfaces as a dependency-validation 400 rather than a plain
        /// validation error, and why it is worth reaching over HTTP at all: no unit test of the
        /// service would exercise it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnPostIfContentTypeIsSetForANonContentItemAsync()
        {
            // given
            ApprovalSetting invalidApprovalSetting = CreateRandomApprovalSetting();
            invalidApprovalSetting.EntityType = EntityType.Tag;
            invalidApprovalSetting.ContentType = ContentType.Story;

            try
            {
                // when
                var postTask =
                    this.apiBroker.PostApprovalSettingAsync(invalidApprovalSetting).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => postTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    invalidApprovalSetting.Id);
            }
        }
    }
}
