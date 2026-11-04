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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Infrastructure;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// What a caller's query options may hold on every <c>[EnableQuery]</c> read, on the running
    /// host (GHSA-3m43-v266-m6qf, Fix task C). The exposers keep writing a bare
    /// <c>[EnableQuery]</c>, and the host narrows every one through
    /// <c>ODataQuerySurfaceConvention</c>.
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public partial class CollectionQuerySurfaceApiTests
    {
        private readonly ApiBroker apiBroker;

        public CollectionQuerySurfaceApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        /// <summary>
        /// C1. Every query attribute the host runs is the narrowed one. Nothing else here fails
        /// on a read the convention missed, because each probe names one route.
        /// </summary>
        [Fact]
        public void ShouldNarrowEveryQueryableCollectionReadsQueryAttribute()
        {
            // given . when
            IReadOnlyList<Type> actualQueryAttributeTypes =
                this.apiBroker.GetQueryableCollectionQueryAttributeTypes();

            // then
            actualQueryAttributeTypes.Should().NotBeEmpty();

            // Asserted over the types rather than over the attributes, so a failure prints the
            // type that was not narrowed.
            actualQueryAttributeTypes.Should().OnlyContain(
                queryAttributeType => queryAttributeType == typeof(NarrowedEnableQueryAttribute));
        }

        /// <summary>
        /// C2. The search the React app sends (<c>apiBroker.contentItems.ts</c>, its search page)
        /// is still answered. <c>content</c> is non-nullable, so its <c>tolower</c> arrives over
        /// the conversion OData inserts, which the narrowed attribute admits.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerTheSearchTheReactAppSendsOnThePublicContentItemsAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;

            CoreContentItem arrangedContentItem =
                await this.apiBroker.InsertFeedContentItemAsync(createdWhen: now, publishDate: now);

            string term = arrangedContentItem.Title.Split(' ').Last().ToLowerInvariant();

            string searchFilter =
                $"(contains(tolower(title),'{term}')"
                + $" or contains(tolower(content),'{term}')"
                + $" or contains(tolower(author),'{term}'))";

            try
            {
                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage response =
                    await this.apiBroker.GetContentItemsResponseAsync(
                        "api/contentItems/Public"
                            + $"?$filter={Uri.EscapeDataString(searchFilter)}&$skip=0&$top=10");

                // then
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                List<Guid> actualContentItemIds = await ReadIdsAsync(response);
                actualContentItemIds.Should().Contain(arrangedContentItem.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
            }
        }

        /// <summary>
        /// C3. An ordering by one of the entity's own properties, descending, is still answered,
        /// and applied. The later item is arranged first, so the order the rows were written in
        /// is not the order asked for.
        /// </summary>
        [Fact]
        public async Task ShouldOrderThePublicContentItemsByCreatedWhenDescendingAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset anHourAgo = now.AddHours(-1);
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                CoreContentItem laterContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(createdWhen: now, publishDate: now);

                arrangedContentItems.Add(laterContentItem);

                CoreContentItem earlierContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: anHourAgo,
                        publishDate: anHourAgo);

                arrangedContentItems.Add(earlierContentItem);

                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage response =
                    await this.apiBroker.GetContentItemsResponseAsync(
                        $"api/contentItems/Public?$orderby={Uri.EscapeDataString("createdWhen desc")}");

                // then
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                List<Guid> actualArrangedContentItemIds = (await ReadIdsAsync(response))
                    .Where(contentItemId =>
                        contentItemId == laterContentItem.Id || contentItemId == earlierContentItem.Id)
                    .ToList();

                actualArrangedContentItemIds.Should().Equal(laterContentItem.Id, earlierContentItem.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                foreach (CoreContentItem arrangedContentItem in arrangedContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
                }
            }
        }

        /// <summary>
        /// C10. The settings list's own filter (<c>apiBroker.contentItemSettings.ts</c>) is still
        /// answered. <c>contentItemId ne null</c> compares a nullable <c>Guid</c> with the
        /// conversion OData inserts over <c>null</c>. The default is asserted absent too, so a
        /// filter that was accepted but not applied would fail.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerAFilterForTheContentItemOverridesAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string userId = Guid.NewGuid().ToString();

            CoreContentItemSetting defaultContentItemSetting =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Story);

            CoreContentItem arrangedContentItem =
                await this.apiBroker.InsertFeedContentItemAsync(createdWhen: now, publishDate: now);

            CoreContentItemSetting arrangedOverride = null;

            try
            {
                arrangedOverride = await this.apiBroker.InsertCoreContentItemSettingAsync(
                    new CoreContentItemSetting
                    {
                        Id = Guid.NewGuid(),
                        ContentType = ContentType.Story,
                        ContentItemId = arrangedContentItem.Id,
                        CreatedBy = userId,
                        CreatedWhen = now,
                        UpdatedBy = userId,
                        UpdatedWhen = now,
                        IsDeleted = false
                    });

                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators);

                // when
                HttpResponseMessage response =
                    await this.apiBroker.GetContentItemSettingsResponseAsync(
                        $"$filter={Uri.EscapeDataString("contentItemId ne null")}");

                // then
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                List<Guid> actualContentItemSettingIds = await ReadIdsAsync(response);
                actualContentItemSettingIds.Should().Contain(arrangedOverride.Id);
                actualContentItemSettingIds.Should().NotContain(defaultContentItemSetting.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (arrangedOverride is not null)
                {
                    await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(arrangedOverride.Id);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
            }
        }

        private static async ValueTask<List<Guid>> ReadIdsAsync(HttpResponseMessage response)
        {
            string payload = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(payload);

            return document.RootElement
                .EnumerateArray()
                .Select(row => row.GetProperty("id").GetGuid())
                .ToList();
        }
    }
}
