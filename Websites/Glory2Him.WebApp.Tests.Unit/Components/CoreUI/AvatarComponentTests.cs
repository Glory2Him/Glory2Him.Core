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

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class AvatarComponentTests : BunitContext
    {
        [Theory]
        [InlineData("Christo du Toit", "CT")]
        [InlineData("Admin", "AD")]
        [InlineData("User", "US")]
        [InlineData("mary-jane watson", "MW")]
        [InlineData("x", "X")]
        public void ShouldRenderInitialsWhenNoImage(string name, string expectedInitials)
        {
            // given . when
            IRenderedComponent<AvatarComponent> renderedAvatar =
                Render<AvatarComponent>(parameters => parameters.Add(a => a.Name, name));

            // then
            renderedAvatar.FindAll("img").Should().BeEmpty();
            renderedAvatar.Find("span[role='img']").TextContent.Trim()
                .Should().Be(expectedInitials);
        }

        [Fact]
        public void ShouldRenderImageWhenImageUrlProvided()
        {
            // given
            const string imageUrl = "Profile-Image/abc?v=1234";

            // when
            IRenderedComponent<AvatarComponent> renderedAvatar =
                Render<AvatarComponent>(parameters => parameters
                    .Add(a => a.Name, "Admin")
                    .Add(a => a.ImageUrl, imageUrl));

            // then
            renderedAvatar.Find("img.avatar-img").GetAttribute("src").Should().Be(imageUrl);
            renderedAvatar.FindAll("span[role='img']").Should().BeEmpty();
        }

        [Fact]
        public void ShouldPickDeterministicColorForSameName()
        {
            // given . when
            IRenderedComponent<AvatarComponent> first =
                Render<AvatarComponent>(parameters => parameters.Add(a => a.Name, "Glory"));

            IRenderedComponent<AvatarComponent> second =
                Render<AvatarComponent>(parameters => parameters.Add(a => a.Name, "Glory"));

            // then (same name → same background colour on every render)
            string firstStyle = first.Find("span[role='img']").GetAttribute("style") ?? "";
            string secondStyle = second.Find("span[role='img']").GetAttribute("style") ?? "";

            firstStyle.Should().Contain("background-color");
            firstStyle.Should().Be(secondStyle);
        }

        [Fact]
        public void ShouldSizeTheAvatar()
        {
            // given . when
            IRenderedComponent<AvatarComponent> renderedAvatar =
                Render<AvatarComponent>(parameters => parameters
                    .Add(a => a.Name, "Admin")
                    .Add(a => a.SizePx, 96));

            // then
            renderedAvatar.Find("div.avatar").GetAttribute("style")
                .Should().Contain("96px");
        }
    }
}
