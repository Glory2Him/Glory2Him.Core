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

using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ButtonComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        [Fact]
        public void ShouldRenderWithDefaultPrimaryColor()
        {
            // given . when
            IRenderedComponent<Button> renderedButton = Render<Button>();

            // then
            renderedButton.Find("button").ClassList.Should().Contain("btn-primary");
            renderedButton.Find("button").GetAttribute("type").Should().Be("button");
        }

        [Fact]
        public void ShouldApplyColorAndChildContent()
        {
            // given
            string randomText = GetRandomString();

            // when
            IRenderedComponent<Button> renderedButton =
                Render<Button>(parameters => parameters
                    .Add(button => button.Color, "success")
                    .AddChildContent(randomText));

            // then
            renderedButton.Find("button").ClassList.Should().Contain("btn-success");
            renderedButton.Markup.Should().Contain(randomText);
        }

        [Fact]
        public void ShouldRenderAsDisabled()
        {
            // given . when
            IRenderedComponent<Button> renderedButton =
                Render<Button>(parameters =>
                    parameters.Add(button => button.Disabled, true));

            // then
            renderedButton.Find("button").HasAttribute("disabled").Should().BeTrue();
        }

        [Fact]
        public void ShouldInvokeOnClickWhenClicked()
        {
            // given
            bool wasClicked = false;

            IRenderedComponent<Button> renderedButton =
                Render<Button>(parameters =>
                    parameters.Add(button => button.OnClick,
                        Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                            this, () => wasClicked = true)));

            // when
            renderedButton.Find("button").Click();

            // then
            wasClicked.Should().BeTrue();
        }
    }
}
