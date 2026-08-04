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
using Glory2Him.WebApp.Models.Views.Products;
using Microsoft.AspNetCore.Components;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ProductCardComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static ProductView CreateRandomProduct() =>
            new ProductView
            {
                Id = GetRandomString(),
                Name = GetRandomString(),
                Slug = GetRandomString(),
                Description = GetRandomString(),
                ImageUrl = "assets/images/shop/01.png",
                Price = 19.99m,
                Rating = 4.5,
                Badge = "New Arrival",
                BadgeCss = "text-bg-success",
            };

        [Fact]
        public void ShouldRenderNamePriceAndLink()
        {
            // given
            ProductView product = CreateRandomProduct();
            string expectedHref = $"Shop-Detail/{product.Slug}";

            // when
            IRenderedComponent<ProductCardComponent> renderedCard =
                Render<ProductCardComponent>(parameters =>
                    parameters.Add(card => card.Product, product));

            // then
            renderedCard.Markup.Should().Contain(product.Name);
            renderedCard.Markup.Should().Contain(product.Price.ToString("C"));
            renderedCard.Find("h5.card-title a").GetAttribute("href").Should().Be(expectedHref);
        }

        [Fact]
        public void ShouldRenderBadgeWhenPresent()
        {
            // given
            ProductView product = CreateRandomProduct();
            product.Badge = "Sale";
            product.BadgeCss = "text-bg-danger";

            // when
            IRenderedComponent<ProductCardComponent> renderedCard =
                Render<ProductCardComponent>(parameters =>
                    parameters.Add(card => card.Product, product));

            // then
            renderedCard.Find("span.badge").ClassList.Should().Contain("text-bg-danger");
            renderedCard.Find("span.badge").TextContent.Should().Contain("Sale");
        }

        [Fact]
        public void ShouldNotRenderBadgeWhenAbsent()
        {
            // given
            ProductView product = CreateRandomProduct();
            product.Badge = null;

            // when
            IRenderedComponent<ProductCardComponent> renderedCard =
                Render<ProductCardComponent>(parameters =>
                    parameters.Add(card => card.Product, product));

            // then
            renderedCard.FindAll("span.badge").Should().BeEmpty();
        }

        [Fact]
        public void ShouldInvokeOnAddToCartWithProduct()
        {
            // given
            ProductView product = CreateRandomProduct();
            ProductView captured = null;

            IRenderedComponent<ProductCardComponent> renderedCard =
                Render<ProductCardComponent>(parameters => parameters
                    .Add(card => card.Product, product)
                    .Add(card => card.OnAddToCart,
                        EventCallback.Factory.Create<ProductView>(
                            this, value => captured = value)));

            // when
            renderedCard.Find("button").Click();

            // then
            captured.Should().BeSameAs(product);
        }
    }
}
