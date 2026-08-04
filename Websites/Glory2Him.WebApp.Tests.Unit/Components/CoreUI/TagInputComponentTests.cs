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
using Microsoft.AspNetCore.Components.Web;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class TagInputComponentTests : BunitContext
    {
        private IReadOnlyList<string> tags = new List<string>();

        private IRenderedComponent<TagInputComponent> RenderTagInput() =>
            Render<TagInputComponent>(parameters => parameters
                .Add(input => input.Tags, this.tags)
                .Add(input => input.TagsChanged, updated => this.tags = updated));

        private static void PressEnter(IRenderedComponent<TagInputComponent> input, string text)
        {
            input.Find("input").Input(text);
            input.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        }

        [Fact]
        public void ShouldStartAsJustTheBox()
        {
            // when
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // then
            input.Find("input[type='text']").Should().NotBeNull();
            input.FindAll("span.g2h-tag-pill").Should().BeEmpty();
        }

        [Fact]
        public void ShouldBuildTheListUpOnEnter()
        {
            // given
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // when
            PressEnter(input, "grace");

            // then
            this.tags.Should().Equal("grace");
        }

        [Fact]
        public void ShouldDropALeadingHashAndTheSurroundingSpace()
        {
            // given
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // when (the hash is how people write a tag, not part of the tag)
            PressEnter(input, "  #grace  ");

            // then
            this.tags.Should().Equal("grace");
        }

        [Fact]
        public void ShouldIgnoreAnEmptyOrRepeatedTag()
        {
            // given
            this.tags = new List<string> { "grace" };
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // when
            PressEnter(input, "   ");
            PressEnter(input, "GRACE");

            // then
            this.tags.Should().Equal("grace");
        }

        [Fact]
        public void ShouldNotAddOnKeysOtherThanEnter()
        {
            // given
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // when
            input.Find("input").Input("grace");
            input.Find("input").KeyDown(new KeyboardEventArgs { Key = "a" });

            // then
            this.tags.Should().BeEmpty();
        }

        [Fact]
        public void ShouldShowEachTagAsAPillCarryingARedCross()
        {
            // given
            this.tags = new List<string> { "grace", "prayer" };

            // when
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // then
            input.FindAll("span.g2h-tag-pill").Cast<IElement>()
                .Select(pill => pill.TextContent.Trim())
                .Should().Equal("grace", "prayer");

            IElement cross = input.Find("span.g2h-tag-pill button.g2h-tag-remove");
            cross.GetAttribute("aria-label").Should().Be("Remove grace");
            cross.QuerySelector("i.bi-x-lg").Should().NotBeNull();
        }

        [Fact]
        public void ShouldRemoveOnlyTheTagWhoseCrossIsClicked()
        {
            // given
            this.tags = new List<string> { "grace", "prayer", "hope" };
            IRenderedComponent<TagInputComponent> input = RenderTagInput();

            // when
            input.FindAll("button.g2h-tag-remove")[1].Click();

            // then
            this.tags.Should().Equal("grace", "hope");
        }

        [Fact]
        public void ShouldAllowATagAgainAfterItIsRemoved()
        {
            // given (the repeat guard must not outlive the pill it was guarding)
            this.tags = new List<string> { "grace" };
            IRenderedComponent<TagInputComponent> input = RenderTagInput();
            input.Find("button.g2h-tag-remove").Click();

            // when
            input = RenderTagInput();
            PressEnter(input, "grace");

            // then
            this.tags.Should().Equal("grace");
        }
    }
}
