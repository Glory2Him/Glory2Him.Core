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
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ArticleComponentsTests : BunitContext
    {
        private static readonly DateTimeOffset Published =
            new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void ShouldRenderTheVerseOfTheDayStrip()
        {
            // when
            IRenderedComponent<VerseOfTheDayComponent> rendered =
                Render<VerseOfTheDayComponent>(parameters =>
                    parameters.Add(verse => verse.Verse, "\"For by grace…\" — Ephesians 2:8 NIV"));

            // then
            rendered.Find("span.badge").TextContent.Trim().Should().Be("Verse of the day:");
            rendered.Markup.Should().Contain("Ephesians 2:8 NIV");
        }

        [Fact]
        public void ShouldFlagTheFeaturedHeroWithAStar()
        {
            // when
            IRenderedComponent<PostHeroCardComponent> rendered =
                Render<PostHeroCardComponent>(parameters => parameters
                    .Add(card => card.Title, "NASA Proves The Bible Is True")
                    .Add(card => card.Category, "Testimony")
                    .Add(card => card.PublishedDate, Published)
                    .Add(card => card.IsFeatured, true));

            // then
            rendered.Find("span.card-featured").Should().NotBeNull();
            rendered.Find("div.card").ClassList.Should().Contain("card-grid-lg");
        }

        [Fact]
        public void ShouldLeaveTheStarOffAnUnfeaturedHero()
        {
            // when
            IRenderedComponent<PostHeroCardComponent> rendered =
                Render<PostHeroCardComponent>(parameters => parameters
                    .Add(card => card.Title, "A quieter story")
                    .Add(card => card.PublishedDate, Published)
                    .Add(card => card.SizeCssClass, "card-grid-sm"));

            // then
            rendered.FindAll("span.card-featured").Should().BeEmpty();
            rendered.Find("div.card").ClassList.Should().Contain("card-grid-sm");
        }

        [Fact]
        public void ShouldRenderHashtagsAndBibleReferencesAsDistinctPills()
        {
            // when
            IRenderedComponent<TagPillListComponent> rendered =
                Render<TagPillListComponent>(parameters => parameters
                    .Add(pills => pills.Tags, new List<string> { "grace", "faith" })
                    .Add(pills => pills.BibleReferences, new List<string> { "Romans 3:23–24" }));

            // then (tags carry a hash, references carry the book icon)
            List<IElement> links = rendered.FindAll("a").ToList();
            links.Should().HaveCount(3);
            links[0].TextContent.Trim().Should().Be("#grace");
            links[2].QuerySelector("i.bi-book").Should().NotBeNull();
            links[2].TextContent.Should().Contain("Romans 3:23–24");
        }

        [Fact]
        public void ShouldSendTagsToASearchAndReferencesToThePassage()
        {
            // when
            IRenderedComponent<TagPillListComponent> rendered =
                Render<TagPillListComponent>(parameters => parameters
                    .Add(pills => pills.Tags, new List<string> { "grace" })
                    .Add(pills => pills.BibleReferences, new List<string> { "Romans 3:23–24" }));

            // then (a tag asks "show me posts about this"; a reference is the passage itself. The
            // reference page shows one fixed verse for now, so its link carries no query rather
            // than one it would ignore)
            List<IElement> links = rendered.FindAll("a").ToList();

            links[0].GetAttribute("href").Should().Be("/Search?q=grace");
            links[1].GetAttribute("href").Should().Be("/BibleReferences");
        }

        [Fact]
        public void ShouldOmitEngagementCountsThatWereNotSupplied()
        {
            // when
            IRenderedComponent<EngagementMetaComponent> rendered =
                Render<EngagementMetaComponent>(parameters => parameters
                    .Add(meta => meta.AuthorName, "Louis Ferguson")
                    .Add(meta => meta.PublishedDate, Published)
                    .Add(meta => meta.Reactions, 257));

            // then (a missing count is left out rather than rendered as a zero)
            rendered.Markup.Should().Contain("257");
            rendered.FindAll("i.fa-comment").Should().BeEmpty();
            rendered.FindAll("i.fa-eye").Should().BeEmpty();
            rendered.Markup.Should().NotContain(">0<");
        }

        [Fact]
        public void ShouldRenderEveryEngagementCountWhenAllAreSupplied()
        {
            // when
            IRenderedComponent<EngagementMetaComponent> rendered =
                Render<EngagementMetaComponent>(parameters => parameters
                    .Add(meta => meta.AuthorName, "Louis Ferguson")
                    .Add(meta => meta.PublishedDate, Published)
                    .Add(meta => meta.Reactions, 257)
                    .Add(meta => meta.Comments, 4)
                    .Add(meta => meta.TagCount, 4)
                    .Add(meta => meta.ReferenceCount, 2)
                    .Add(meta => meta.Views, 2344));

            // then
            rendered.FindAll("li.nav-item").Should().HaveCount(7);
            rendered.Markup.Should().Contain("2344 Views");
        }

        [Fact]
        public void ShouldRaiseTheReactionCallbackWhenAReactionIsClicked()
        {
            // given
            ReactionOption? reacted = null;

            var reactions = new List<ReactionOption>
            {
                new ReactionOption("Amen", "fas fa-thumbs-up", "#4e5ff9", 112),
                new ReactionOption("Love", "fas fa-heart", "#d6293e", 98),
            };

            IRenderedComponent<ReactionBarComponent> rendered =
                Render<ReactionBarComponent>(parameters => parameters
                    .Add(bar => bar.Reactions, reactions)
                    .Add(bar => bar.OnReact, option => reacted = option));

            // when
            rendered.FindAll("button.reaction-btn")[1].Click();

            // then
            reacted!.Label.Should().Be("Love");
        }

        [Fact]
        public void ShouldIndentReplyCommentsOnly()
        {
            // given
            var comments = new List<CommentEntry>
            {
                new CommentEntry("Allen Smith", null, Published, "This blessed me.", 14),
                new CommentEntry("Louis Ferguson", null, Published, "Thank you Allen.", 6,
                    IsReply: true),
            };

            // when
            IRenderedComponent<CommentThreadComponent> rendered =
                Render<CommentThreadComponent>(parameters =>
                    parameters.Add(thread => thread.Comments, comments));

            // then
            rendered.Find("h3").TextContent.Should().Contain("2 comments");

            List<IElement> rows = rendered.FindAll("div.d-flex").ToList();
            rows[0].ClassList.Should().NotContain("ps-md-5");
            rows[1].ClassList.Should().Contain("ps-md-5");
        }

        [Fact]
        public void ShouldRunTheBylineAlongOneLine()
        {
            // when
            IRenderedComponent<AuthorBylineComponent> rendered =
                Render<AuthorBylineComponent>(parameters => parameters
                    .Add(byline => byline.AuthorName, "Louis Ferguson")
                    .Add(byline => byline.AuthorRole, "An editor at Glory 2 Him")
                    .Add(byline => byline.PublishedDate, Published)
                    .Add(byline => byline.ReadMinutes, 5)
                    .Add(byline => byline.Reactions, 257)
                    .Add(byline => byline.Comments, 4)
                    .Add(byline => byline.Views, 2344));

            // then (one wrapping flex row, not a stacked column)
            rendered.Find("div").ClassList.Should()
                .Contain(new[] { "d-flex", "flex-wrap", "align-items-center" });

            rendered.Markup.Should().Contain("Louis Ferguson");
            rendered.Markup.Should().Contain("An editor at Glory 2 Him");
            rendered.Markup.Should().Contain("5 min read");
            rendered.Markup.Should().Contain("257 reactions");
            rendered.Markup.Should().Contain("4 comments");
            rendered.Markup.Should().Contain("2344 Views");
        }

        [Fact]
        public void ShouldMarkReadingTimeWithTheFilledClock()
        {
            // when
            IRenderedComponent<AuthorBylineComponent> rendered =
                Render<AuthorBylineComponent>(parameters => parameters
                    .Add(byline => byline.AuthorName, "Louis Ferguson")
                    .Add(byline => byline.ReadMinutes, 5));

            // then (the filled clock, not the outline one)
            IElement readingTime = rendered.FindAll("li.nav-item")
                .First(item => item.TextContent.Contains("min read"));

            readingTime.QuerySelector("i.bi-clock-fill").Should().NotBeNull();
        }

        [Fact]
        public void ShouldOmitBylineFiguresThatWereNotSupplied()
        {
            // when
            IRenderedComponent<AuthorBylineComponent> rendered =
                Render<AuthorBylineComponent>(parameters => parameters
                    .Add(byline => byline.AuthorName, "Louis Ferguson")
                    .Add(byline => byline.PublishedDate, Published));

            // then
            rendered.FindAll("li.nav-item").Should().ContainSingle();
            rendered.Markup.Should().NotContain("min read");
            rendered.Markup.Should().NotContain("Views");
        }

        [Fact]
        public void ShouldRenderTheArticleCardWithItsTagsAndByline()
        {
            // when
            IRenderedComponent<ArticleCardComponent> rendered =
                Render<ArticleCardComponent>(parameters => parameters
                    .Add(card => card.Title, "Justification means there isn't a charge against you")
                    .Add(card => card.Excerpt, "Your sins are completely wiped out.")
                    .Add(card => card.Category, "Quotes")
                    .Add(card => card.CategoryBadgeCss, "text-bg-success")
                    .Add(card => card.AuthorName, "Bryan Knight")
                    .Add(card => card.PublishedDate, Published)
                    .Add(card => card.Tags, new List<string> { "justified", "grace" })
                    .Add(card => card.BibleReferences, new List<string> { "Romans 3:23–24" })
                    .Add(card => card.Reactions, 142));

            // then
            rendered.Find("a.badge").ClassList.Should().Contain("text-bg-success");
            rendered.Markup.Should().Contain("#justified");
            rendered.Markup.Should().Contain("Romans 3:23–24");
            rendered.Markup.Should().Contain("by Bryan Knight");
            rendered.Markup.Should().Contain("142");
        }
    }
}
