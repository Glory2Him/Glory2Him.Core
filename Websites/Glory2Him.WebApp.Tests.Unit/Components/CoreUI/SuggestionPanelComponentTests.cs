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
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class SuggestionPanelComponentTests : BunitContext
    {
        private IRenderedComponent<SuggestionPanelComponent> RenderTagPanel() =>
            Render<SuggestionPanelComponent>(parameters => parameters
                .Add(panel => panel.Heading, "Tags")
                .Add(panel => panel.SuggestHeading, "Suggest a tag")
                .Add(panel => panel.Prompt, "Think a tag is missing?")
                .Add(panel => panel.Items, new List<string> { "creation", "science" })
                .Add(panel => panel.PrefixHash, true));

        private static void Suggest(IRenderedComponent<SuggestionPanelComponent> panel, string text)
        {
            panel.Find("input").Input(text);
            panel.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
            {
                Key = "Enter",
            });
        }

        [Fact]
        public void ShouldRenderTheHeadingAboveTheSuggestSubHeading()
        {
            // when
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // then (the set's own name is the h4; suggesting is a smaller sub-heading beneath it)
            panel.Find("h4").TextContent.Trim().Should().Be("Tags");

            IElement subHeading = panel.Find("p.text-uppercase.fw-bold");
            subHeading.TextContent.Trim().Should().Be("Suggest a tag");
            subHeading.ClassList.Should().Contain("small");
        }

        [Fact]
        public void ShouldPrefixTagsWithAHash()
        {
            // when
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // then
            panel.FindAll("a.g2h-suggest-pill").Select(a => a.TextContent.Trim())
                .Should().Equal("#creation", "#science");
        }

        [Fact]
        public void ShouldShowBibleReferencesWithABookIconAndNoHash()
        {
            // when
            IRenderedComponent<SuggestionPanelComponent> panel =
                Render<SuggestionPanelComponent>(parameters => parameters
                    .Add(p => p.Heading, "Bible references")
                    .Add(p => p.Items, new List<string> { "Joshua 10:8, 12–13" })
                    .Add(p => p.ItemIconCssClass, "bi-book")
                    .Add(p => p.HrefFormat, "Search-Result?q={0}"));

            // then
            IElement pill = panel.Find("a.g2h-suggest-pill");
            pill.QuerySelector("i.bi-book").Should().NotBeNull();
            pill.TextContent.Trim().Should().Be("Joshua 10:8, 12–13");
            pill.GetAttribute("href").Should().StartWith("Search-Result?q=");
        }

        [Fact]
        public void ShouldAppendASuggestionAtTheEndMarkedPending()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            Suggest(panel, "miracles");

            // then (approved pills stay put; the suggestion lands after them, awaiting approval)
            panel.FindAll("a.g2h-suggest-pill").Should().HaveCount(2);

            IElement pending = panel.Find("span.g2h-suggest-pending");
            pending.TextContent.Trim().Should().Be("#miracles");
            pending.QuerySelector("i.bi-hourglass-split").Should().NotBeNull();
            pending.GetAttribute("title").Should().Be("Pending approval");
        }

        [Fact]
        public void ShouldClearTheBoxAfterASuggestion()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            Suggest(panel, "miracles");

            // then
            panel.Find("input").GetAttribute("value").Should().BeEmpty();
        }

        [Fact]
        public void ShouldIgnoreASuggestionThatIsAlreadyListed()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when (same tag, different casing and with the hash the user might type)
            Suggest(panel, "#Creation");

            // then
            panel.FindAll("span.g2h-suggest-pending").Should().BeEmpty();
        }

        [Fact]
        public void ShouldIgnoreARepeatedSuggestion()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            Suggest(panel, "miracles");
            Suggest(panel, "miracles");

            // then
            panel.FindAll("span.g2h-suggest-pending").Should().ContainSingle();
        }

        [Fact]
        public void ShouldIgnoreAnEmptySuggestion()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            Suggest(panel, "   ");

            // then
            panel.FindAll("span.g2h-suggest-pending").Should().BeEmpty();
        }

        [Fact]
        public void ShouldNotSuggestOnKeysOtherThanEnter()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            panel.Find("input").Input("miracles");
            panel.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
            {
                Key = "a",
            });

            // then
            panel.FindAll("span.g2h-suggest-pending").Should().BeEmpty();
        }

        [Fact]
        public void ShouldOfferACrossOnPendingPillsOnly()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();

            // when
            Suggest(panel, "miracles");

            // then (an approved pill is not the reader's to withdraw)
            panel.FindAll("a.g2h-suggest-pill button").Should().BeEmpty();

            IElement remove = panel.Find("span.g2h-suggest-pending button.g2h-suggest-remove");
            remove.GetAttribute("title").Should().Be("Remove suggestion");
            remove.GetAttribute("aria-label").Should().Be("Remove #miracles");
            remove.QuerySelector("i.bi-x-lg").Should().NotBeNull();
        }

        [Fact]
        public void ShouldRemoveASuggestionWhenItsCrossIsClicked()
        {
            // given
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();
            Suggest(panel, "miracles");
            Suggest(panel, "grace");

            // when
            panel.FindAll("button.g2h-suggest-remove")[0].Click();

            // then (only the one clicked goes; the approved pills are untouched)
            panel.FindAll("span.g2h-suggest-pending").Select(pill => pill.TextContent.Trim())
                .Should().Equal("#grace");

            panel.FindAll("a.g2h-suggest-pill").Should().HaveCount(2);
        }

        [Fact]
        public void ShouldAllowASuggestionAgainAfterItIsRemoved()
        {
            // given (the repeat guard must not outlive the pill it was guarding)
            IRenderedComponent<SuggestionPanelComponent> panel = RenderTagPanel();
            Suggest(panel, "miracles");
            panel.Find("button.g2h-suggest-remove").Click();

            // when
            Suggest(panel, "miracles");

            // then
            panel.Find("span.g2h-suggest-pending").TextContent.Trim().Should().Be("#miracles");
        }

        [Fact]
        public void ShouldRaiseTheSuggestedCallback()
        {
            // given
            string? suggested = null;

            IRenderedComponent<SuggestionPanelComponent> panel =
                Render<SuggestionPanelComponent>(parameters => parameters
                    .Add(p => p.Heading, "Tags")
                    .Add(p => p.Items, new List<string>())
                    .Add(p => p.OnSuggested, value => suggested = value));

            // when
            Suggest(panel, "grace");

            // then
            suggested.Should().Be("grace");
        }
    }
}
