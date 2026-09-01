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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ContentItemSettings
{
    /// <summary>
    /// Proves a seeded SortOrder survives the insert — issue #395.
    ///
    /// <para>Quote is seeded 0, the first tile in the contribution picker, and 0 is the CLR
    /// default for an int. A store default marks the property ValueGenerated.OnAdd and EF then
    /// omits any value matching the property's sentinel, so the one value the curated order
    /// most wants was the one value the column default replaced with 1000 — on every database
    /// created after the backfill migration, and on none created before it.</para>
    ///
    /// <para>These go through the real broker because nothing else can see it. The unit suite
    /// mocks the storage broker, so the layer that drops the value is stubbed out; the model
    /// guard beside it asserts the configuration, and this asserts what SQL Server then
    /// stored.</para>
    /// </summary>
    [Collection(ContentItemSettingCollection.Name)]
    public sealed class ContentItemSettingSortOrderTests : IDisposable
    {
        private readonly ContentItemSettingQueryBroker broker;
        private readonly List<Guid> seededContentItemSettingIds;

        public ContentItemSettingSortOrderTests(ContentItemSettingQueryBroker broker)
        {
            this.broker = broker;
            this.seededContentItemSettingIds = new List<Guid>();
        }

        [Fact]
        public async Task ShouldStoreASortOrderOfZeroOnInsertAsync()
        {
            // given
            ContentItemSetting quoteDefault =
                ContentItemSettingQueryBroker.CreateDefaultSetting(ContentType.Quote);

            quoteDefault.SortOrder = 0;
            this.seededContentItemSettingIds.Add(quoteDefault.Id);

            // when
            await this.broker.InsertAsync(quoteDefault);

            int storedSortOrder =
                await this.broker.GetStoredSortOrderAsync(quoteDefault.Id);

            // then
            storedSortOrder.Should().Be(0,
                because: "the value the caller set is the value the column keeps, even when it " +
                    "is the CLR default the store default used to replace (#395)");
        }

        [Fact]
        public async Task ShouldStoreTheSortOrderTheEntityCarriesWhenNothingSetsItAsync()
        {
            // given: the entity's own default matches the column's, so a row nobody ordered
            // still lands past the curated seed values rather than in front of them.
            ContentItemSetting unorderedDefault =
                ContentItemSettingQueryBroker.CreateDefaultSetting(ContentType.Story);

            this.seededContentItemSettingIds.Add(unorderedDefault.Id);

            // when
            await this.broker.InsertAsync(unorderedDefault);

            int storedSortOrder =
                await this.broker.GetStoredSortOrderAsync(unorderedDefault.Id);

            // then
            storedSortOrder.Should().Be(1000,
                because: "an unordered row sorts after every type somebody chose the order of");
        }

        [Fact]
        public async Task ShouldFallBackToTheColumnDefaultWhenTheInsertNamesNoSortOrderAsync()
        {
            // given: the column default is kept alongside ValueGeneratedNever rather than
            // dropped, because a raw-SQL insert names no SortOrder and the NOT NULL column
            // would reject it. This is the caller the default still serves.
            Guid contentItemSettingId = Guid.NewGuid();
            this.seededContentItemSettingIds.Add(contentItemSettingId);

            // when
            await this.broker.InsertNamingNoSortOrderAsync(
                contentItemSettingId, ContentType.Testimony);

            int storedSortOrder =
                await this.broker.GetStoredSortOrderAsync(contentItemSettingId);

            // then
            storedSortOrder.Should().Be(1000,
                because: "the column default is what an insert naming no column falls to");
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededContentItemSettingIds)
                .AsTask().GetAwaiter().GetResult();
    }
}
