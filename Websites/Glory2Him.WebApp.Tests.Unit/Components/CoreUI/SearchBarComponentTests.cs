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
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class SearchBarComponentTests : BunitContext
    {
        [Fact]
        public void ShouldPairTheBoxWithAGreenSearchButton()
        {
            // when
            IRenderedComponent<SearchBarComponent> bar =
                Render<SearchBarComponent>(parameters =>
                    parameters.Add(search => search.Placeholder, "Search posts"));

            // then
            IElement box = bar.Find("input[type='search']");
            box.GetAttribute("placeholder").Should().Be("Search posts");
            box.ClassList.Should().Contain("border-success");

            IElement button = bar.Find("button[type='submit']");
            button.ClassList.Should().Contain("btn-success");
            button.TextContent.Trim().Should().Be("Search");
            button.QuerySelector("i.bi-search").Should().NotBeNull();
        }

        [Fact]
        public void ShouldHideTheChevronWhenThereAreNoAdvancedOptions()
        {
            // when
            IRenderedComponent<SearchBarComponent> bar = Render<SearchBarComponent>();

            // then (a chevron that opens nothing is worse than no chevron)
            bar.FindAll("button[type='button']").Should().BeEmpty();
        }

        [Fact]
        public void ShouldFoldTheAdvancedOptionsOutAndBackOnTheChevron()
        {
            // given
            IRenderedComponent<SearchBarComponent> bar = RenderWithAdvanced();
            IElement chevron = bar.Find("button[type='button']");

            chevron.GetAttribute("aria-expanded").Should().Be("false");
            bar.FindAll("#advancedSearchOptions").Should().BeEmpty();

            // when
            chevron.Click();

            // then
            bar.Find("#advancedSearchOptions").TextContent.Should().Contain("Any category");

            bar.Find("button[type='button']").GetAttribute("aria-expanded").Should().Be("true");
            bar.Find("button[type='button'] i").ClassList.Should().Contain("bi-chevron-up");

            // when (closed again)
            bar.Find("button[type='button']").Click();

            // then
            bar.FindAll("#advancedSearchOptions").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRaiseTheQueryAsItIsTyped()
        {
            // given
            string? typed = null;

            IRenderedComponent<SearchBarComponent> bar =
                Render<SearchBarComponent>(parameters =>
                    parameters.Add(search => search.QueryChanged, value => typed = value));

            // when
            bar.Find("input[type='search']").Input("grace");

            // then
            typed.Should().Be("grace");
        }

        [Fact]
        public void ShouldRaiseTheSearchOnSubmit()
        {
            // given
            bool searched = false;

            IRenderedComponent<SearchBarComponent> bar =
                Render<SearchBarComponent>(parameters =>
                    parameters.Add(search => search.OnSearch, () => searched = true));

            // when
            bar.Find("form").Submit();

            // then
            searched.Should().BeTrue();
        }

        private IRenderedComponent<SearchBarComponent> RenderWithAdvanced() =>
            Render<SearchBarComponent>(parameters => parameters
                .Add(search => search.Advanced, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "option");
                    builder.AddContent(1, "Any category");
                    builder.CloseElement();
                })));
    }
}
