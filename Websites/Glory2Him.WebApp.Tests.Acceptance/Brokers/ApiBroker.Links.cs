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
using Glory2Him.WebApp.Tests.Acceptance.Models.Links;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string linksRelativeUrl = "api/links";

        public async ValueTask<Link> PostLinkAsync(Link link) =>
            await this.apiFactoryClient.PostContentAsync(linksRelativeUrl, link);

        public async ValueTask<List<Link>> GetAllLinksAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<Link>>($"{linksRelativeUrl}/");

        public async ValueTask<List<Link>> GetSpecificLinkByIdAsync(Guid linkId) =>
            await this.apiFactoryClient.GetContentAsync<List<Link>>(
                $"{linksRelativeUrl}?$filter=Id eq {linkId}");

        public async ValueTask<Link> GetLinkByIdAsync(Guid linkId) =>
            await this.apiFactoryClient.GetContentAsync<Link>($"{linksRelativeUrl}/{linkId}");

        public async ValueTask<Link> DeleteLinkByIdAsync(Guid linkId) =>
            await this.apiFactoryClient.DeleteContentAsync<Link>($"{linksRelativeUrl}/{linkId}");




        public async ValueTask<Link> PutLinkAsync(Link link) =>
            await this.apiFactoryClient.PutContentAsync(linksRelativeUrl, link);

        public async ValueTask<List<Link>> GetPublicLinksAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<Link>>(
                $"{linksRelativeUrl}/Public");

        public async ValueTask<List<Link>> GetLinksByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<List<Link>>(
                $"{linksRelativeUrl}/Groups/{groupId}");

        public async ValueTask<Link> GetLatestLinkByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<Link>(
                $"{linksRelativeUrl}/Groups/{groupId}/Latest");

        public async ValueTask<Link> GetPublishedLinkByGroupIdAsync(Guid groupId) =>
            await this.apiFactoryClient.GetContentAsync<Link>(
                $"{linksRelativeUrl}/Groups/{groupId}/Published");
    }
}
