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

using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public partial class NewsletterComponentTests
    {
        [Fact]
        public void ShouldRenderDefaultHeadingAndButton()
        {
            // given . when
            IRenderedComponent<NewsletterComponent> renderedNewsletter =
                Render<NewsletterComponent>();

            // then
            renderedNewsletter.Find("h2").TextContent.Should().Be("Join our community");
            renderedNewsletter.Find("button[type=submit]").TextContent.Should().Contain("Subscribe");
            renderedNewsletter.FindAll("input[type=email]").Should().HaveCount(1);
        }

        [Fact]
        public void ShouldRenderCustomHeadingSubheadingAndButton()
        {
            // given
            string randomHeading = GetRandomString();
            string randomSubheading = GetRandomString();
            string randomButtonText = GetRandomString();

            // when
            IRenderedComponent<NewsletterComponent> renderedNewsletter =
                Render<NewsletterComponent>(parameters => parameters
                    .Add(newsletter => newsletter.Heading, randomHeading)
                    .Add(newsletter => newsletter.Subheading, randomSubheading)
                    .Add(newsletter => newsletter.ButtonText, randomButtonText));

            // then
            renderedNewsletter.Markup.Should().Contain(randomHeading);
            renderedNewsletter.Markup.Should().Contain(randomSubheading);
            renderedNewsletter.Markup.Should().Contain(randomButtonText);
        }
    }
}
