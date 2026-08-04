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

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Components.Pages.SamplePages;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class PostSinglePageComponentTests
    {
        [Fact]
        public void ShouldOpenWithTheTitleAndTheHorizontalByline()
        {
            // when
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // then
            renderedPage.Find("h1").TextContent.Trim()
                .Should().Be(SampleContent.Featured.Title);

            renderedPage.Find("a.badge").TextContent.Trim()
                .Should().Be(SampleContent.Featured.Category);

            renderedPage.Markup.Should().Contain(SampleContent.DetailAuthorName);
        }

        [Fact]
        public void ShouldSetTheArticleBesideTheSidebarWithTheCommentsBelowBoth()
        {
            // when
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // then (two rows: article + sidebar, then the conversation on its own — which is what
            // puts the sidebar between the reactions and the comments once the columns stack)
            renderedPage.Find("div.col-lg-7 span.dropcap").Should().NotBeNull();
            renderedPage.Find("div.col-lg-7 .reaction-btn").Should().NotBeNull();

            renderedPage.FindAll("div.col-lg-5 h4").Cast<IElement>()
                .Select(heading => heading.TextContent.Trim())
                .Should().Equal(
                    "Tags", "Bible references", "Have something to share?", "Share this article");

            renderedPage.FindAll("div.row").Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public void ShouldCarryTheTagsAndBibleReferencesOfThePost()
        {
            // when
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // then
            renderedPage.FindAll("a.g2h-suggest-pill").Cast<IElement>()
                .Select(pill => pill.TextContent.Trim())
                .Should().Equal(
                    SampleContent.Featured.Tags.Select(tag => $"#{tag}")
                        .Concat(SampleContent.Featured.BibleReferences));
        }

        [Fact]
        public void ShouldRespondToAReaction()
        {
            // given
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // when
            renderedPage.FindAll("button.reaction-btn")[0].Click();

            // then
            renderedPage.Find("p.text-body-secondary strong").TextContent.Trim()
                .Should().Be(SampleContent.Reactions[0].Label);
        }

        [Fact]
        public void ShouldPointEveryLinkAtAPublicRoute()
        {
            // when
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // then (the sample this mirrors sends its bible references to an Administrators-only
            // page — a visitor following one from here would be bounced to a login)
            renderedPage.Markup.Should().NotContain("SamplePages");
        }

        [Fact]
        public void ShouldSendTagsToASearchAndReferencesToThePassage()
        {
            // when
            IRenderedComponent<PostSingle> renderedPage = Render<PostSingle>();

            // then
            List<string> pillLinks = renderedPage.FindAll("a.g2h-suggest-pill")
                .Select(pill => pill.GetAttribute("href")!)
                .ToList();

            int tags = SampleContent.Featured.Tags.Count;

            pillLinks.Take(tags).Should().OnlyContain(href => href.StartsWith("/Search?q="));
            pillLinks.Skip(tags).Should().OnlyContain(href => href == "/BibleReferences");
        }

        [Fact]
        public void ShouldRenderTheSameStoryWhateverSlugIsAskedFor()
        {
            // when (no posts are wired in yet, so the slug is accepted and ignored)
            IRenderedComponent<PostSingle> renderedPage =
                Render<PostSingle>(parameters =>
                    parameters.Add(page => page.Slug, "some-other-post"));

            // then
            renderedPage.Find("h1").TextContent.Trim()
                .Should().Be(SampleContent.Featured.Title);
        }
    }
}
