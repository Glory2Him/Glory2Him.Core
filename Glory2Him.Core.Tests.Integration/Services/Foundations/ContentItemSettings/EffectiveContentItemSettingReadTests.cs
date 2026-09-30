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
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ContentItemSettings
{
    /// <summary>
    /// Proves <see cref="IAccessBroker.RetrieveEffectiveContentItemSettingsAsync"/> against a
    /// real catalogue (§ARC12.2.1 rule 6).
    ///
    /// <para>The unit suite settles §DOM6.4 precedence by applying the gather's query-shaping
    /// function to an in-memory set. What only SQL Server can say is that the function
    /// <b>translates</b>: one ordered, top-one subquery per key, joined by a set operation, with
    /// each key's values and the <c>ContentType</c> string conversion carried into the query.
    /// EF refuses an untranslatable query at run time, so LINQ-to-Objects passing is no evidence
    /// the database was ever asked the same question.</para>
    ///
    /// <para>The <c>AccessBroker</c> is built over the fixture's REAL storage broker, because a
    /// mock is exactly what would let an untranslatable query pass.</para>
    /// </summary>
    [Collection(ContentItemSettingCollection.Name)]
    public sealed class EffectiveContentItemSettingReadTests : IDisposable
    {
        private readonly ContentItemSettingQueryBroker broker;
        private readonly IAccessBroker accessBroker;
        private readonly List<Guid> seededContentItemSettingIds;

        public EffectiveContentItemSettingReadTests(ContentItemSettingQueryBroker broker)
        {
            this.broker = broker;
            this.accessBroker = new AccessBroker(broker.StorageBroker);
            this.seededContentItemSettingIds = new List<Guid>();
        }

        [Fact]
        public async Task ShouldSelectEveryKeysEffectiveSettingInSqlAsync()
        {
            // given: two types, each with a deleted row, and four items asked about in one call.
            //  - an item with its own live override, beside its type's live default (criterion 1)
            //  - an item with no override, while another item's override is live (criterion 2)
            //  - an item whose only override is deleted (criterion 3)
            //  - an item of a type whose only default is deleted (criterion 4)
            Guid overriddenItemId = Guid.NewGuid();
            Guid defaultedItemId = Guid.NewGuid();
            Guid deletedOverrideItemId = Guid.NewGuid();
            Guid unresolvedItemId = Guid.NewGuid();

            ContentItemSetting testimonyDefault =
                await SeedAsync(ContentType.Testimony, contentItemId: null);

            ContentItemSetting itemOverride =
                await SeedAsync(ContentType.Testimony, overriddenItemId);

            await SeedAsync(ContentType.Testimony, deletedOverrideItemId, isDeleted: true);
            await SeedAsync(ContentType.Devotional, contentItemId: null, isDeleted: true);

            var contentItemSettingKeys = new List<ContentItemSettingKey>
            {
                CreateKey(ContentType.Testimony, overriddenItemId),
                CreateKey(ContentType.Testimony, defaultedItemId),
                CreateKey(ContentType.Testimony, deletedOverrideItemId),
                CreateKey(ContentType.Devotional, unresolvedItemId),
            };

            var expectedEffectiveContentItemSettings = new List<EffectiveContentItemSetting>
            {
                CreateEffective(overriddenItemId, itemOverride),
                CreateEffective(defaultedItemId, testimonyDefault),
                CreateEffective(deletedOverrideItemId, testimonyDefault),
            };

            // when
            IReadOnlyList<EffectiveContentItemSetting> actualEffectiveContentItemSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    contentItemSettingKeys: contentItemSettingKeys,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualEffectiveContentItemSettings.Should().BeEquivalentTo(
                expectedEffectiveContentItemSettings,
                because: "SQL Server must select each key's row as §DOM6.4 does in memory: the "
                    + "live override, else the live default, and nothing for an item neither "
                    + "resolves");
        }

        private async Task<ContentItemSetting> SeedAsync(
            ContentType contentType,
            Guid? contentItemId,
            bool isDeleted = false)
        {
            ContentItemSetting contentItemSetting =
                ContentItemSettingQueryBroker.CreateDefaultSetting(contentType);

            contentItemSetting.ContentItemId = contentItemId;
            contentItemSetting.IsDeleted = isDeleted;
            this.seededContentItemSettingIds.Add(contentItemSetting.Id);
            await this.broker.InsertAsync(contentItemSetting);

            return contentItemSetting;
        }

        private static ContentItemSettingKey CreateKey(ContentType contentType, Guid contentItemId) =>
            new ContentItemSettingKey
            {
                ContentType = contentType,
                ContentItemId = contentItemId,
            };

        private static EffectiveContentItemSetting CreateEffective(
            Guid contentItemId,
            ContentItemSetting contentItemSetting) =>
            new EffectiveContentItemSetting
            {
                ContentItemId = contentItemId,
                ContentItemSetting = contentItemSetting,
            };

        public void Dispose() =>
            this.broker.ClearAsync(this.seededContentItemSettingIds)
                .AsTask().GetAwaiter().GetResult();
    }
}
