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
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class CardComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        [Fact]
        public void ShouldRenderTitleAndBody()
        {
            // given
            string randomTitle = GetRandomString();
            string randomBody = GetRandomString();

            // when
            IRenderedComponent<Card> renderedCard =
                Render<Card>(parameters => parameters
                    .Add(card => card.Title, randomTitle)
                    .AddChildContent(randomBody));

            // then
            renderedCard.Find("div.card-header").TextContent.Should().Contain(randomTitle);
            renderedCard.Find("div.card-body").TextContent.Should().Contain(randomBody);
        }

        [Fact]
        public void ShouldNotRenderHeaderWhenNoTitleOrHeaderContent()
        {
            // given
            string randomBody = GetRandomString();

            // when
            IRenderedComponent<Card> renderedCard =
                Render<Card>(parameters => parameters.AddChildContent(randomBody));

            // then
            renderedCard.FindAll("div.card-header").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRenderFooterContentWhenSupplied()
        {
            // given
            string randomFooter = GetRandomString();

            // when
            IRenderedComponent<Card> renderedCard =
                Render<Card>(parameters => parameters
                    .Add(card => card.FooterContent, builder => builder.AddContent(0, randomFooter)));

            // then
            renderedCard.Find("div.card-footer").TextContent.Should().Contain(randomFooter);
        }
    }
}
