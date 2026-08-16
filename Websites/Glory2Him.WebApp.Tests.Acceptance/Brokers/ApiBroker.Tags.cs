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
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string tagsRelativeUrl = "api/tags";

        public async ValueTask<Tag> PostTagAsync(Tag tag) =>
            await this.apiFactoryClient.PostContentAsync(tagsRelativeUrl, tag);

        public async ValueTask<List<Tag>> GetAllTagsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<Tag>>($"{tagsRelativeUrl}/");

        public async ValueTask<List<Tag>> GetSpecificTagByIdAsync(Guid tagId) =>
            await this.apiFactoryClient.GetContentAsync<List<Tag>>(
                $"{tagsRelativeUrl}?$filter=Id eq {tagId}");

        public async ValueTask<Tag> GetTagByIdAsync(Guid tagId) =>
            await this.apiFactoryClient.GetContentAsync<Tag>($"{tagsRelativeUrl}/{tagId}");

        public async ValueTask<Tag> DeleteTagByIdAsync(Guid tagId) =>
            await this.apiFactoryClient.DeleteContentAsync<Tag>($"{tagsRelativeUrl}/{tagId}");

        public async ValueTask<Tag> HardDeleteTagByIdAsync(Guid tagId) =>
            await this.apiFactoryClient.DeleteContentAsync<Tag>($"{tagsRelativeUrl}/{tagId}/hard");

        public async ValueTask<Tag> ApproveTagAsync(Tag tag) =>
            await this.apiFactoryClient.PostContentAsync($"{tagsRelativeUrl}/approve", tag);

        public async ValueTask<Tag> SubmitTagByIdAsync(Guid tagId) =>
            await this.apiFactoryClient.PostContentAsync<object, Tag>(
                $"{tagsRelativeUrl}/{tagId}/submit",
                content: new object());

        public async ValueTask<Tag> PutTagAsync(Tag tag) =>
            await this.apiFactoryClient.PutContentAsync(tagsRelativeUrl, tag);
    }
}
