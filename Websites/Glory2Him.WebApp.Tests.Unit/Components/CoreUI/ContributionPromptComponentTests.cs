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

using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ContributionPromptComponentTests : BunitContext
    {
        [Fact]
        public void ShouldSitInABorderedRoundedPanel()
        {
            // when
            IRenderedComponent<ContributionPromptComponent> rendered =
                Render<ContributionPromptComponent>();

            // then (the theme's own border, so it matches the rules around it)
            rendered.Find("div").ClassList.Should().Contain(new[] { "border", "rounded-3" });
        }

        [Fact]
        public void ShouldInviteAContributionWithAnActionableLink()
        {
            // when
            IRenderedComponent<ContributionPromptComponent> rendered =
                Render<ContributionPromptComponent>(parameters =>
                    parameters.Add(prompt => prompt.Href, "Contribute"));

            // then
            rendered.Find("h4").TextContent.Trim().Should().Be("Have something to share?");

            rendered.Find("p").TextContent.Should()
                .Contain("if it might encourage someone else");

            IElement link = rendered.Find("a");
            link.TextContent.Trim().Should().Be("Submit a contribution");
            link.GetAttribute("href").Should().Be("Contribute");
        }

        [Fact]
        public void ShouldSizeThePencilJustUnderAnAuthorAvatar()
        {
            // when
            IRenderedComponent<ContributionPromptComponent> rendered =
                Render<ContributionPromptComponent>();

            // then (the byline avatar is 44px, so the icon sits a little below it)
            IElement icon = rendered.Find("i.bi-pencil-square");

            icon.GetAttribute("style").Should().Contain("36px");
            icon.GetAttribute("aria-hidden").Should().Be("true");
        }

        [Fact]
        public void ShouldFloatTheIconInsideTheBodySoTheHeadingKeepsItsFullWidth()
        {
            // when
            IRenderedComponent<ContributionPromptComponent> rendered =
                Render<ContributionPromptComponent>();

            // then (the icon opens the body paragraph, the way a dropcap does — a float there
            // shortens only the lines beneath it, leaving the heading above at full width)
            IElement panel = rendered.Find("div.g2h-contribute");

            // Indexed rather than projected with LINQ: bUnit puts a Select(IElement, EventArgs)
            // event-dispatch extension in scope, which shadows LINQ's on an IHtmlCollection.
            panel.Children.Length.Should().Be(3);
            panel.Children[0].TagName.Should().Be("H4");
            panel.Children[1].TagName.Should().Be("P");
            panel.Children[2].TagName.Should().Be("A");

            IElement body = panel.Children[1];
            body.FirstElementChild!.TagName.Should().Be("I");
            body.FirstElementChild.ClassList.Should().Contain("g2h-contribute-icon");
        }

        [Fact]
        public void ShouldLetTheCallerReplaceEveryPieceOfCopy()
        {
            // when
            IRenderedComponent<ContributionPromptComponent> rendered =
                Render<ContributionPromptComponent>(parameters => parameters
                    .Add(prompt => prompt.Heading, "Got a story?")
                    .Add(prompt => prompt.Body, "Send it over.")
                    .Add(prompt => prompt.LinkText, "Share it")
                    .Add(prompt => prompt.Href, "Share"));

            // then
            rendered.Find("h4").TextContent.Trim().Should().Be("Got a story?");
            rendered.Find("p").TextContent.Trim().Should().Be("Send it over.");
            rendered.Find("a").TextContent.Trim().Should().Be("Share it");
        }
    }
}
