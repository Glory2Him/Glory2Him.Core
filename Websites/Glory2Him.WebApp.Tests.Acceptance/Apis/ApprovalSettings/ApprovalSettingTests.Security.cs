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
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalSettings
{
    /// <summary>
    /// §14.7 posture C, and this suite is written rather than copied because posture C differs
    /// from the content exposers' posture A in both directions.
    ///
    /// <para><b>Writes are narrower.</b> Every write including hard removal is <c>Administrators</c> only.
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
        /// reads on <c>Administrators</c> like the writes; the design says otherwise, because a contributor
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
        /// A publisher is the interesting negative — they carry the highest tier this
        /// exposer's sibling controllers admit on their approve route, and it buys them nothing
        /// here.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.Publishers)]
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
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.Publishers)]
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
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.Publishers)]
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
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.Publishers)]
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
        /// hoped-for.
        ///
        /// <para>Nothing is arranged: the incumbent is the default <c>ApprovalSettingSeedData</c>
        /// seeds for every entity type at startup, so the duplicate collides with the seeded row.
        /// Arranging one of our own would be refused as the very conflict this asserts, and a
        /// test whose arrangement is what fails is a test that reads as an exposer regression.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfEntityTypeAlreadyHasADefaultAsync()
        {
            // given
            ApprovalSetting duplicateApprovalSetting = CreateRandomApprovalSetting();
            duplicateApprovalSetting.EntityType = EntityType.Tag;
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
                // A no-op when the post was refused as it should be, and the teardown that
                // matters when it was not.
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

        /// <summary>
        /// The INVERSE of the two conflict tests above, and the sharper half of #326.
        ///
        /// <para>Both indexes now carry an <c>IsDeleted</c> term, so removing a policy through the
        /// API — a SOFT delete — genuinely releases its scope. Without the term the scope was
        /// trapped: §14.5 hides the deleted row from every caller including
        /// <c>Administrators</c>, so the
        /// re-create answered 409 naming nothing anybody could see or move, and with eight
        /// <c>EntityType</c> members holding one default slot each, the ability to have a default
        /// for that entity type was destroyed permanently.</para>
        ///
        /// <para>Asserted rather than assumed, mirroring
        /// <c>ShouldAllowPostWhenUsfmIsHeldOnlyByASoftDeletedRowAsync</c> on
        /// <c>BibleReference</c>. If either filter is ever narrowed back, this test fails.</para>
        /// </summary>
        /// <para>This is the default tier, so the slot has to be freed before anything can be
        /// written to it — every entity type's default is taken at startup by
        /// <c>ApprovalSettingSeedData</c>. The seeded row is lifted out physically and put back
        /// byte-for-byte in the teardown; the seed restores a missing LIVE default on the next
        /// startup, but nothing restarts mid-suite, so the teardown is what the following tests
        /// depend on. The predecessor is arranged beneath HTTP because the tier it needs to sit
        /// in is the one the suite can no longer post to.</para>
        [Fact]
        public async Task ShouldAllowPostWhenEntityTypeDefaultIsHeldOnlyByASoftDeletedRowAsync()
        {
            // given
            CoreApprovalSetting seededDefault =
                await this.apiBroker.GetCoreDefaultApprovalSettingAsync(EntityType.Link);

            CoreApprovalSetting softDeletedDefault =
                CreateSoftDeletedCoreDefaultApprovalSetting(EntityType.Link);

            ApprovalSetting reusedScopeApprovalSetting = CreateRandomApprovalSetting();
            reusedScopeApprovalSetting.EntityType = EntityType.Link;
            reusedScopeApprovalSetting.ContentType = null;

            // The predecessor goes in while the seeded row still holds the slot, which is only
            // safe because the index constrains live rows alone — the arrangement and the
            // assertion rest on the same term. The seeded row then leaves the slot on the LAST
            // line before the try, so every call that could throw while the slot is empty is
            // covered by the restore in the finally.
            await this.apiBroker.InsertCoreApprovalSettingAsync(softDeletedDefault);
            await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(seededDefault.Id);

            try
            {
                // when
                ApprovalSetting actualApprovalSetting =
                    await this.apiBroker.PostApprovalSettingAsync(reusedScopeApprovalSetting);

                // then
                actualApprovalSetting.EntityType.Should().Be(EntityType.Link);
                actualApprovalSetting.ContentType.Should().BeNull();
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(softDeletedDefault.Id);

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    reusedScopeApprovalSetting.Id);

                await this.apiBroker.InsertCoreApprovalSettingAsync(seededDefault);
            }
        }

        /// <summary>
        /// The same release, on the second index. Written rather than folded into the test above
        /// because the two filters are separate strings in the configuration and a fix applied to
        /// one and missed on the other is exactly the shape of the original defect.
        /// </summary>
        [Fact]
        public async Task ShouldAllowPostWhenEntityTypeContentTypePairIsHeldOnlyByASoftDeletedRowAsync()
        {
            // given
            ApprovalSetting overrideApprovalSetting = CreateRandomApprovalSetting();

            // ContentItem, and not by preference:
            // CK_ApprovalSetting_ContentTypeRequiresContentItem permits a populated ContentType
            // only on that entity type (design §8.4, §18.6 rule 5).
            overrideApprovalSetting.EntityType = EntityType.ContentItem;
            overrideApprovalSetting.ContentType = ContentType.Devotional;

            ApprovalSetting removedOverride =
                await this.apiBroker.PostApprovalSettingAsync(overrideApprovalSetting);

            await this.apiBroker.DeleteApprovalSettingByIdAsync(removedOverride.Id);

            ApprovalSetting reusedPairApprovalSetting = CreateRandomApprovalSetting();
            reusedPairApprovalSetting.EntityType = EntityType.ContentItem;
            reusedPairApprovalSetting.ContentType = ContentType.Devotional;

            try
            {
                // when
                ApprovalSetting actualApprovalSetting =
                    await this.apiBroker.PostApprovalSettingAsync(reusedPairApprovalSetting);

                // then
                actualApprovalSetting.EntityType.Should().Be(EntityType.ContentItem);
                actualApprovalSetting.ContentType.Should().Be(ContentType.Devotional);
            }
            finally
            {
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(removedOverride.Id);

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                    reusedPairApprovalSetting.Id);
            }
        }
    }
}
