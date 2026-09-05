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
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItemSettings
{
    /// <summary>
    /// §14.7 posture C, and this suite is written rather than copied because posture C differs
    /// from the content exposers' posture A in both directions.
    ///
    /// <para><b>Writes are narrower, and split by the SHAPE OF THE ROW.</b> A per-type default
    /// (<c>ContentItemId</c> null) is <c>Administrators</c> only; an item override additionally
    /// admits the publisher tier for that row's content type (§12.5.2 business rule 6, and the
    /// §18.6 rule 1 exception it rests on). There is still no owner branch — nobody "owns" a
    /// setting — so the owner-versus-moderator cases the posture A suites spend most of their
    /// length on do not exist here.</para>
    ///
    /// <para><b>A refusal is 401 here, not 403</b>, and the change is worth naming: the role list
    /// has left the <c>[Authorize]</c> attribute, because neither the row-shaped rule nor the
    /// one-role-per-content-type narrow tier can be expressed on one. An authenticated caller now
    /// reaches the service and is refused by it, which surfaces through
    /// <c>UnauthorizedContentItemSettingException</c> as a 401 — the same shape
    /// <c>ContentItemsController</c> has always had for a service-decided gate.</para>
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
        // Pinned rather than drawn: a narrow-tier assertion that let the filler pick the content
        // type would pass whenever the draw happened to agree with the role, proving nothing.
        private const ContentType PublisherTierContentType = ContentType.Devotional;
        private const ContentType OtherPublisherTierContentType = ContentType.Quote;

        public static TheoryData<string> PublisherTierRoles() =>
            new TheoryData<string>
            {
                Roles.Publishers,
                Roles.ContentItemPublishers,
                Roles.PublishersFor(EntityType.ContentItem, PublisherTierContentType)
            };

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
        /// THE OTHER HALF OF THE ROW-SHAPED RULE, over real HTTP. The publisher tier may author an
        /// item override — the write this whole change exists to allow — and is refused a per-type
        /// default in the same breath. Both directions in one place, because a rule proven only by
        /// its refusals is a rule that might be refusing everything.
        ///
        /// <para>The three tiers are asserted separately: the global <c>Publishers</c>, the
        /// entity-scoped <c>ContentItem-Publishers</c>, and the narrow
        /// <c>ContentItem-%ContentType%-Publishers</c> for the row's own type. A hole in any one
        /// of them is a role somebody holds that silently buys nothing.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(PublisherTierRoles))]
        public async Task ShouldAllowPublisherTierToPostAnOverrideAsync(string roleName)
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentType = PublisherTierContentType;
            ContentItemSetting expectedContentItemSetting = randomContentItemSetting;
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

            ContentItemSetting actualContentItemSetting = null;

            try
            {
                // when
                actualContentItemSetting =
                    await this.apiBroker.PostContentItemSettingAsync(randomContentItemSetting);

                // then
                actualContentItemSetting.Id.Should().Be(expectedContentItemSetting.Id);
                actualContentItemSetting.ContentItemId.Should()
                    .Be(expectedContentItemSetting.ContentItemId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (actualContentItemSetting is not null)
                {
                    await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                        actualContentItemSetting.Id);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PublisherTierRoles))]
        public async Task ShouldRefusePublisherTierAPerTypeDefaultAsync(string roleName)
        {
            // given: the same caller, the same content type, one field different — the scope
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentType = PublisherTierContentType;
            randomContentItemSetting.ContentItemId = null;
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

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
        /// A publisher of ONE content type has no authority over another's overrides — the narrow
        /// tier is narrow, which is the whole reason it exists.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseNarrowPublisherAnOverrideOfAnotherContentTypeAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentType = PublisherTierContentType;

            this.apiBroker.ActAs(
                Guid.NewGuid().ToString(),
                Roles.PublishersFor(EntityType.ContentItem, OtherPublisherTierContentType));

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
        /// The authenticated read still works. Asserted alongside the anonymous one so that a
        /// change gating these on <c>Administrators</c> — the natural mistake, since every WRITE here is
        /// <c>Administrators</c> — fails on both.
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
        /// A reviewer holds the highest tier that buys NOTHING here: the review tier is excluded
        /// from publisher authority everywhere (§8.6 HR-3), and settings admit no reviewer of
        /// their own. The publisher cases moved out of this theory when the row-shaped rule
        /// landed — a publisher may now write an override, and is covered by its own pair of
        /// tests below.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewers)]
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
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
            }
        }

        [Theory]
        [InlineData(Roles.Reviewers)]
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
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewers)]
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
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    existingContentItemSetting.Id);
            }
        }

        [Theory]
        [InlineData(Roles.Reviewers)]
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
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
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
            // given: the host seeds one default per content type at startup
            // (ContentItemSettingSeedData), so every type already holds the slot this post wants.
            // Nothing is arranged here — the incumbent is the real seeded row, which makes this a
            // truer test of the rule than a default this suite planted for itself.
            ContentItemSetting duplicateContentItemSetting = CreateRandomContentItemSetting();
            duplicateContentItemSetting.ContentType = ContentType.Quote;
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
                // The post was refused, so there is nothing of this suite's to tear down — and
                // the seeded default must survive, or every later run loses its incumbent.
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

        /// <summary>
        /// The INVERSE of the two conflict tests above, and the point of #326.
        ///
        /// <para>Both indexes now carry an <c>IsDeleted</c> term, so a soft-deleted row genuinely
        /// releases its scope. Without the term the scope was trapped: §14.5 hides the deleted row
        /// from every caller including <c>Administrators</c>, so the re-create answered 409 naming
        /// nothing anybody could see or move.</para>
        ///
        /// <para><b>The predecessor is arranged BENEATH HTTP, and has to be.</b> #387 makes the
        /// delete endpoint refuse a default outright — every content type must always have one
        /// (§12.5.2 business rule 5) — so the API can no longer produce a soft-deleted default and
        /// the row is written through the storage broker instead. The assertion stays worth
        /// making for exactly that reason: the index term is the defence in depth behind the
        /// service's refusal, and a soft-deleted default can still arrive by the routes the
        /// service does not own — a direct write, a restore, a future bulk operation.</para>
        ///
        /// <para>This is the default tier, so the slot has to be freed before anything can be
        /// written to it — every content type's default is taken at startup. The seeded row is
        /// lifted out physically and put back byte-for-byte in the teardown; the seed now restores
        /// a missing LIVE default on the next startup, but nothing restarts mid-suite, so the
        /// teardown is what the following tests depend on.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAllowPostWhenContentTypeDefaultIsHeldOnlyByASoftDeletedRowAsync()
        {
            // given
            CoreContentItemSetting seededDefault =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Topic);

            CoreContentItemSetting softDeletedDefault =
                CreateSoftDeletedCoreDefaultContentItemSetting(ContentType.Topic);

            ContentItemSetting reusedScopeContentItemSetting = CreateRandomContentItemSetting();
            reusedScopeContentItemSetting.ContentType = ContentType.Topic;
            reusedScopeContentItemSetting.ContentItemId = null;

            // The predecessor goes in while the seeded row still holds the slot, which is only
            // safe because the index constrains live rows alone — the arrangement and the
            // assertion rest on the same term, and if the term were missing this insert would be
            // the line that failed. The seeded row then leaves the slot on the LAST line before
            // the try, so every call that could throw while the slot is empty is covered by the
            // restore in the finally. The ids are minted here rather than by the responses, so
            // teardown reaches a row whose post never returned.
            await this.apiBroker.InsertCoreContentItemSettingAsync(softDeletedDefault);
            await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(seededDefault.Id);

            try
            {
                // when
                ContentItemSetting actualContentItemSetting = await this.apiBroker
                    .PostContentItemSettingAsync(reusedScopeContentItemSetting);

                // then
                actualContentItemSetting.ContentType.Should().Be(ContentType.Topic);
                actualContentItemSetting.ContentItemId.Should().BeNull();
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(softDeletedDefault.Id);

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    reusedScopeContentItemSetting.Id);

                await this.apiBroker.InsertCoreContentItemSettingAsync(seededDefault);
            }
        }

        /// <summary>
        /// The same release, on the second index. Written rather than folded into the test above
        /// because the two filters are separate strings in the configuration and a fix applied to
        /// one and missed on the other is exactly the shape of the original defect — and because
        /// this tier needs no seed juggling at all, a fresh <c>ContentItemId</c> being an
        /// unlimited supply of free scopes.
        /// </summary>
        [Fact]
        public async Task ShouldAllowPostWhenContentItemOverrideIsHeldOnlyByASoftDeletedRowAsync()
        {
            // given
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting overrideContentItemSetting = CreateRandomContentItemSetting();
            overrideContentItemSetting.ContentItemId = contentItemId;

            ContentItemSetting removedOverride = await this.apiBroker
                .PostContentItemSettingAsync(overrideContentItemSetting);

            await this.apiBroker.DeleteContentItemSettingByIdAsync(removedOverride.Id);

            ContentItemSetting reusedScopeContentItemSetting = CreateRandomContentItemSetting();
            reusedScopeContentItemSetting.ContentItemId = contentItemId;

            try
            {
                // when
                ContentItemSetting actualContentItemSetting = await this.apiBroker
                    .PostContentItemSettingAsync(reusedScopeContentItemSetting);

                // then
                actualContentItemSetting.ContentItemId.Should().Be(contentItemId);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(removedOverride.Id);

                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(
                    reusedScopeContentItemSetting.Id);
            }
        }
    }
}
