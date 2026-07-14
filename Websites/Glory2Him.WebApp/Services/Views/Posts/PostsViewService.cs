// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Views.Posts;

namespace Glory2Him.WebApp.Services.Views.Posts
{
    public partial class PostsViewService : IPostsViewService
    {
        private readonly ILoggingBroker loggingBroker;

        public PostsViewService(ILoggingBroker loggingBroker) =>
            this.loggingBroker = loggingBroker;

        public ValueTask<List<PostView>> RetrieveAllPostsAsync() =>
            TryCatch(() => new ValueTask<List<PostView>>(SamplePosts.All));

        public ValueTask<PostView> RetrievePostBySlugAsync(string slug) =>
            TryCatch(() =>
            {
                PostView post =
                    SamplePosts.All.FirstOrDefault(post =>
                        string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase))
                            ?? SamplePosts.All.First();

                return new ValueTask<PostView>(post);
            });

        public ValueTask<PostView> RetrievePostByIdAsync(string id) =>
            TryCatch(() =>
            {
                PostView post =
                    SamplePosts.All.First(post =>
                        string.Equals(post.Id, id, StringComparison.OrdinalIgnoreCase));

                return new ValueTask<PostView>(post);
            });

        public ValueTask<PostView> AddPostAsync(PostView post) =>
            TryCatch(() =>
            {
                post.Id = SamplePosts.NextId();
                post.Slug = Slugify(post.Title, post.Id);
                SamplePosts.All.Add(post);

                return new ValueTask<PostView>(post);
            });

        public ValueTask<PostView> ModifyPostAsync(PostView post) =>
            TryCatch(() =>
            {
                PostView existing =
                    SamplePosts.All.First(current =>
                        string.Equals(current.Id, post.Id, StringComparison.OrdinalIgnoreCase));

                existing.Title = post.Title;
                existing.Slug = Slugify(post.Title, post.Id);
                existing.Excerpt = post.Excerpt;
                existing.ImageUrl = post.ImageUrl;
                existing.Category = post.Category;
                existing.CategoryBadgeCss = post.CategoryBadgeCss;
                existing.AuthorName = post.AuthorName;
                existing.AuthorImageUrl = post.AuthorImageUrl;
                existing.PublishedDate = post.PublishedDate;
                existing.ReadMinutes = post.ReadMinutes;
                existing.IsFeatured = post.IsFeatured;

                return new ValueTask<PostView>(existing);
            });

        public ValueTask RemovePostAsync(string id) =>
            TryCatch(() =>
            {
                PostView existing =
                    SamplePosts.All.First(current =>
                        string.Equals(current.Id, id, StringComparison.OrdinalIgnoreCase));

                SamplePosts.All.Remove(existing);

                return default;
            });

        private static string Slugify(string title, string id)
        {
            var builder = new System.Text.StringBuilder();

            foreach (char character in title.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
                else if (char.IsWhiteSpace(character) || character == '-')
                {
                    builder.Append('-');
                }
            }

            string slug = builder.ToString().Trim('-');

            while (slug.Contains("--"))
            {
                slug = slug.Replace("--", "-");
            }

            return string.IsNullOrWhiteSpace(slug) ? $"post-{id}" : slug;
        }
    }
}
