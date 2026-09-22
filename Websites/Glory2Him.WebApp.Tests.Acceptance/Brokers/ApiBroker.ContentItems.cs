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
using System.Net.Http;
using System.Threading.Tasks;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string contentItemsRelativeUrl = "api/contentItems";

        public async ValueTask<ContentItem> PostContentItemAsync(ContentItem contentItem) =>
            await this.apiFactoryClient.PostContentAsync(contentItemsRelativeUrl, contentItem);

        public async ValueTask<List<ContentItem>> GetAllContentItemsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>($"{contentItemsRelativeUrl}/");

        public async ValueTask<List<ContentItem>> GetSpecificContentItemByIdAsync(Guid contentItemId) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}?$filter=Id eq {contentItemId}");

        public async ValueTask<ContentItem> GetContentItemByIdAsync(Guid contentItemId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItem>($"{contentItemsRelativeUrl}/{contentItemId}");

        public async ValueTask<ContentItem> DeleteContentItemByIdAsync(Guid contentItemId) =>
            await this.apiFactoryClient.DeleteContentAsync<ContentItem>($"{contentItemsRelativeUrl}/{contentItemId}");




        public async ValueTask<ContentItem> PutContentItemAsync(ContentItem contentItem) =>
            await this.apiFactoryClient.PutContentAsync(contentItemsRelativeUrl, contentItem);

        public async ValueTask<List<ContentItem>> GetPublicContentItemsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Public");

        // THE FEED, and it is a different read from /Public rather than a view of it
        // (§DOM11.3 + §DOM3.8 rule 2). The route carries no [EnableQuery], so the page travels
        // as two plain parameters the read itself accepts — which is why there is no
        // odataQuery overload here and a caller asking for one gets the odd one below, whose
        // whole purpose is to prove the options are ignored.
        public async ValueTask<List<ContentItem>> GetContentItemFeedAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Feed");

        public async ValueTask<List<ContentItem>> GetContentItemFeedAsync(int skip, int take) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Feed?skip={skip}&take={take}");

        public async ValueTask<List<ContentItem>> GetContentItemFeedAsync(string queryString) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Feed?{queryString}");

        // The raw response, for the assertions that are about the STATUS rather than the body -
        // a bare URL answering 200 rather than 400, and the validation arms answering 400.
        public async ValueTask<HttpResponseMessage> GetContentItemFeedResponseAsync(
            string queryString = "")
        {
            string url = string.IsNullOrEmpty(queryString)
                ? $"{contentItemsRelativeUrl}/Feed"
                : $"{contentItemsRelativeUrl}/Feed?{queryString}";

            return await this.httpClient.GetAsync(url);
        }

        public async ValueTask<List<ContentItem>> GetContentItemsByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}");

        // The group read carries [EnableQuery] with a restricted option set: $top/$skip/$count
        // compose, $filter/$orderby are refused. This overload is how a test asks for either.
        public async ValueTask<List<ContentItem>> GetContentItemsByGroupIdAsync(Guid groupId, string odataQuery) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}?{odataQuery}");

        public async ValueTask<ContentItem> GetLatestContentItemByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItem>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}/Latest");

        public async ValueTask<ContentItem> GetPublishedContentItemByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItem>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}/Published");
    }
}
