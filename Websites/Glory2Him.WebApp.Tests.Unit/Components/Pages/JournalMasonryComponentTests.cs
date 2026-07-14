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
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public class JournalMasonryComponentTests : BunitContext
    {
        private readonly Mock<IPostsViewService> postsViewServiceMock;

        public JournalMasonryComponentTests()
        {
            this.postsViewServiceMock = new Mock<IPostsViewService>();
            Services.AddSingleton(this.postsViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static PostView CreatePost(string category) =>
            new PostView
            {
                Id = GetRandomString(),
                Title = GetRandomString(),
                Slug = GetRandomString(),
                ImageUrl = "assets/images/blog/16by9/big/01.jpg",
                Category = category,
                CategoryBadgeCss = "text-bg-primary",
                AuthorName = GetRandomString(),
                AuthorImageUrl = "assets/images/avatar/01.jpg",
                PublishedDate = new DateTimeOffset(2022, 2, 18, 0, 0, 0, TimeSpan.Zero),
            };

        [Fact]
        public void ShouldRenderIsotopeContainerAndAllPostsFilter()
        {
            // given
            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(new List<PostView> { CreatePost("Faith") });

            // when
            IRenderedComponent<JournalMasonry> renderedPage = Render<JournalMasonry>();

            // then (the container/attributes the isotope vendor JS binds to must be present)
            renderedPage.Find("div.filter-container").GetAttribute("data-isotope")
                .Should().Contain("masonry");

            renderedPage.Find("div.grid-menu").GetAttribute("data-target")
                .Should().Be(".filter-container");

            renderedPage.Find("a[data-filter='*']").Should().NotBeNull();
        }

        [Fact]
        public void ShouldRenderOneFilterPerDistinctCategoryWithCssClass()
        {
            // given
            var posts = new List<PostView>
            {
                CreatePost("Faith"),
                CreatePost("Hope"),
                CreatePost("Faith"),
            };

            this.postsViewServiceMock.Setup(service =>
                service.RetrieveAllPostsAsync())
                    .ReturnsAsync(posts);

            // when
            IRenderedComponent<JournalMasonry> renderedPage = Render<JournalMasonry>();

            // then (distinct: Faith + Hope filters, using the "<category>-category" class)
            renderedPage.FindAll("a[data-filter]:not([data-filter='*'])").Should().HaveCount(2);
            renderedPage.Find("a[data-filter='.faith-category']").Should().NotBeNull();
            renderedPage.FindAll("div.grid-item.faith-category").Should().HaveCount(2);
            renderedPage.FindAll("div.grid-item.hope-category").Should().HaveCount(1);
        }
    }
}
