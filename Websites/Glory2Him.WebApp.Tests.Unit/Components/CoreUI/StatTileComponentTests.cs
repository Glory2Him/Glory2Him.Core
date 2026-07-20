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
    public class StatTileComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        public static TheoryData<StatTileVariant, string> VariantIconClasses() =>
            new TheoryData<StatTileVariant, string>
            {
                { StatTileVariant.Green, "text-success" },
                { StatTileVariant.Amber, "text-warning" },
                { StatTileVariant.Red, "text-danger" },
                { StatTileVariant.Na, "text-primary" },
            };

        [Fact]
        public void ShouldDefaultToNaVariant()
        {
            // given . when
            IRenderedComponent<StatTile> renderedTile = Render<StatTile>();

            // then
            renderedTile.Instance.Variant.Should().Be(StatTileVariant.Na);
            renderedTile.Find("div.stat-tile").ClassList.Should().Contain("rag-na");
        }

        [Theory]
        [MemberData(nameof(VariantIconClasses))]
        public void ShouldApplyBlogzineContextualColorForVariant(
            StatTileVariant variant,
            string expectedIconCssClass)
        {
            // given . when
            IRenderedComponent<StatTile> renderedTile =
                Render<StatTile>(parameters => parameters
                    .Add(tile => tile.Variant, variant)
                    .Add(tile => tile.Icon, "bi-people-fill"));

            // then
            renderedTile.Find("div.stat-tile-icon").ClassList
                .Should().Contain(expectedIconCssClass);
        }

        [Fact]
        public void ShouldRenderValueAndLabel()
        {
            // given
            string randomValue = GetRandomString();
            string randomLabel = GetRandomString();

            // when
            IRenderedComponent<StatTile> renderedTile =
                Render<StatTile>(parameters => parameters
                    .Add(tile => tile.Value, randomValue)
                    .Add(tile => tile.Label, randomLabel));

            // then
            renderedTile.Find("h3.stat-tile-value").TextContent.Should().Contain(randomValue);
            renderedTile.Find("h6.stat-tile-label").TextContent.Should().Contain(randomLabel);
        }

        [Fact]
        public void ShouldRenderAsBlogzineCardStyle()
        {
            // given . when
            IRenderedComponent<StatTile> renderedTile = Render<StatTile>();

            // then (restyled from EventHighway's gradient tile onto the Blogzine counter card)
            renderedTile.Find("div.stat-tile").ClassList.Should().Contain("card");
        }
    }
}
