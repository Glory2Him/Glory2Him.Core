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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // §DOM6.4 precedence, in its one home (§ARC16.8, the §DOM6.10 row). The condition is
        // authored here as a query-shaping function, so each test below applies the function the
        // broker hands to storage over an in-memory set (§ARC12.2.1 rule 5): the set carries the
        // row that should win and, for each term, a row that misses on that term alone.
        //
        // Every set lists the row that must LOSE ahead of the one that must win, so a read that
        // took the first match rather than ordering the override first would answer wrongly.
        [Fact]
        public async Task ShouldAnswerTheItemsOwnOverrideWhereOneExistsAsync()
        {
            // given
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting typeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            ContentItemSetting itemOverride =
                CreateContentItemSetting(ContentType.Testimony, contentItemId);

            SetupContentItemSettings(typeDefault, itemOverride);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(contentItemId, itemOverride),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "an item's own live override takes full precedence over its type's "
                    + "default (§DOM6.4)");
        }

        [Fact]
        public async Task ShouldAnswerTheTypeDefaultWhereTheItemHasNoOverrideAsync()
        {
            // given
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting typeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            SetupContentItemSettings(typeDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(contentItemId, typeDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "with no override of its own, an item takes its content type's live "
                    + "default (§DOM6.3)");
        }

        [Fact]
        public async Task ShouldNeverLetADeletedOverrideWinAsync()
        {
            // given
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting deletedItemOverride =
                CreateContentItemSetting(ContentType.Testimony, contentItemId, isDeleted: true);

            ContentItemSetting typeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            SetupContentItemSettings(deletedItemOverride, typeDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(contentItemId, typeDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "a soft-deleted setting is excluded from active policy resolution "
                    + "(§DOM6.6), so the item falls back to its type's default");
        }

        [Fact]
        public async Task ShouldLeaveOutAnItemThatResolvesNoRowAsync()
        {
            // given: the item's type has only a deleted default, and ANOTHER type has a live one.
            // The second row misses on the type alone, so the item comes back absent only when
            // the default is matched on its type as well as on being live.
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting deletedTypeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null, isDeleted: true);

            ContentItemSetting otherTypeDefault =
                CreateContentItemSetting(ContentType.Devotional, contentItemId: null);

            SetupContentItemSettings(deletedTypeDefault, otherTypeDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEmpty(
                because: "an item that resolves no live row is absent from the answer, and what "
                    + "that absence means is its caller's to say");
        }

        [Fact]
        public async Task ShouldNeverApplyOneItemsOverrideToAnotherAsync()
        {
            // given: an override for one item of the type, and a second item of the same type
            // asked about. The override misses on the item alone.
            Guid contentItemId = Guid.NewGuid();
            Guid otherContentItemId = Guid.NewGuid();

            ContentItemSetting otherItemOverride =
                CreateContentItemSetting(ContentType.Testimony, otherContentItemId);

            ContentItemSetting typeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            SetupContentItemSettings(otherItemOverride, typeDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(contentItemId, typeDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "an override applies only to the content item it names (§DOM6.4)");
        }

        [Fact]
        public async Task ShouldMatchAnOverrideOnTheItemAndTheTypeTogetherAsync()
        {
            // given: an override naming the item asked about, but under a different content type
            // from the key's. It misses on the type alone, as resolveContentItemSetting.ts on the
            // client would miss it.
            Guid contentItemId = Guid.NewGuid();

            ContentItemSetting otherTypeItemOverride =
                CreateContentItemSetting(ContentType.Devotional, contentItemId);

            ContentItemSetting typeDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            SetupContentItemSettings(otherTypeItemOverride, typeDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, contentItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(contentItemId, typeDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "an override is matched on the item and its type together, so one "
                    + "filed under another type is not the item's");
        }

        [Fact]
        public async Task ShouldAnswerEveryKeyFromOneQueryAsync()
        {
            // given: two items of two types, neither with an override, and each type with its own
            // default. Each default misses the OTHER key on the type alone.
            Guid testimonyItemId = Guid.NewGuid();
            Guid devotionalItemId = Guid.NewGuid();

            ContentItemSetting devotionalDefault =
                CreateContentItemSetting(ContentType.Devotional, contentItemId: null);

            ContentItemSetting testimonyDefault =
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null);

            SetupContentItemSettings(devotionalDefault, testimonyDefault);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateContentItemSettingKey(ContentType.Testimony, testimonyItemId),
                CreateContentItemSettingKey(ContentType.Devotional, devotionalItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffectiveContentItemSetting(testimonyItemId, testimonyDefault),
                CreateEffectiveContentItemSetting(devotionalItemId, devotionalDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "each item is answered from its own content type");

            // One round trip for every key, so the selection runs in SQL and nothing is picked
            // over in memory (§ARC16.8, the §DOM6.10 row).
            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingsAsync(
                    It.IsAny<Func<IQueryable<ContentItemSetting>, IQueryable<EffectiveContentItemSetting>>>(),
                    TestContext.Current.CancellationToken),
                        Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        // The task's edge cases, beside the criteria they bound.
        [Fact]
        public async Task ShouldAnswerNothingWhenNoKeyIsAskedAsync()
        {
            // given
            SetupContentItemSettings(
                CreateContentItemSetting(ContentType.Testimony, contentItemId: null));

            var contentItemSettingKeys = new List<ContentItemSettingKey>();

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEmpty(
                because: "an empty key list answers an empty list");
        }

        // The function is the one argument matched with It.IsAny, because a function cannot be
        // matched by value. It is proven by applying it instead. The token is matched exactly,
        // so a read that dropped the caller's token answers nothing.
        private void SetupContentItemSettings(params ContentItemSetting[] contentItemSettings) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingsAsync(
                    It.IsAny<Func<IQueryable<ContentItemSetting>, IQueryable<EffectiveContentItemSetting>>>(),
                    TestContext.Current.CancellationToken))
                        .Returns((
                            Func<IQueryable<ContentItemSetting>, IQueryable<EffectiveContentItemSetting>> query,
                            CancellationToken cancellationToken) =>
                                ValueTask.FromResult<IReadOnlyList<EffectiveContentItemSetting>>(
                                    query(contentItemSettings.AsQueryable()).ToList()));

        // The ContentType is always set by the caller, never left to a filler: a type left at its
        // default is the same on every row, and the type term would then go unexercised.
        private static ContentItemSetting CreateContentItemSetting(
            ContentType contentType,
            Guid? contentItemId,
            bool isDeleted = false) =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = contentType,
                ContentItemId = contentItemId,
                IsDeleted = isDeleted,
            };

        private static ContentItemSettingKey CreateContentItemSettingKey(
            ContentType contentType,
            Guid contentItemId) =>
            new ContentItemSettingKey
            {
                ContentType = contentType,
                ContentItemId = contentItemId,
            };

        private static EffectiveContentItemSetting CreateEffectiveContentItemSetting(
            Guid contentItemId,
            ContentItemSetting contentItemSetting) =>
            new EffectiveContentItemSetting
            {
                ContentItemId = contentItemId,
                ContentItemSetting = contentItemSetting,
            };
    }
}
