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
using Bunit;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class SearchResultPageComponentTests : BunitContext
    {
        private readonly Mock<IPostsViewService> postsViewServiceMock;

        public SearchResultPageComponentTests()
        {
            this.postsViewServiceMock = new Mock<IPostsViewService>();
            Services.AddSingleton(this.postsViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static PostView CreatePost(string title, string category) =>
            new PostView
            {
                Id = GetRandomString(),
                Title = title,
                Slug = GetRandomString(),
                Excerpt = GetRandomString(),
                ImageUrl = "assets/images/blog/16by9/big/01.jpg",
                Category = category,
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = GetRandomString(),
                AuthorImageUrl = "assets/images/avatar/01.jpg",
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
                ReadMinutes = 5,
                IsFeatured = false,
            };
    }
}
