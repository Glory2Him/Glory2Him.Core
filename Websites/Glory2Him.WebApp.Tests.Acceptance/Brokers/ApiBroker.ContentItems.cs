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

        public async ValueTask<List<ContentItem>> GetContentItemsByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<List<ContentItem>>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}");

        public async ValueTask<ContentItem> GetLatestContentItemByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItem>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}/Latest");

        public async ValueTask<ContentItem> GetPublishedContentItemByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<ContentItem>(
                $"{contentItemsRelativeUrl}/Groups/{groupId}/Published");
    }
}
