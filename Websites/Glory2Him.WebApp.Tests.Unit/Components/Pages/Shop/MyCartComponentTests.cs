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
using Glory2Him.WebApp.Components.Pages.Shop;
using Glory2Him.WebApp.Models.Views.Products;
using Glory2Him.WebApp.Services.Cart;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Shop
{
    public class MyCartComponentTests : BunitContext
    {
        private readonly ICartService cartService;

        public MyCartComponentTests()
        {
            this.cartService = new CartService();
            Services.AddSingleton(this.cartService);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static ProductView CreateProduct(string id, decimal price) =>
            new ProductView
            {
                Id = id,
                Name = $"Product {id}",
                Slug = $"product-{id}",
                ImageUrl = "assets/images/shop/01.png",
                Price = price,
            };

        [Fact]
        public void ShouldShowEmptyStateWhenCartIsEmpty()
        {
            // given . when
            IRenderedComponent<MyCart> renderedPage = Render<MyCart>();

            // then
            renderedPage.Markup.Should().Contain("Your cart is empty");
        }

        [Fact]
        public void ShouldRenderItemsAndSubtotal()
        {
            // given
            this.cartService.Add(CreateProduct("1", 10m), quantity: 2);
            this.cartService.Add(CreateProduct("2", 5m));

            // when
            IRenderedComponent<MyCart> renderedPage = Render<MyCart>();

            // then
            renderedPage.Markup.Should().Contain("Product 1");
            renderedPage.Markup.Should().Contain("Product 2");
            renderedPage.Markup.Should().Contain(25m.ToString("C"));
        }

        [Fact]
        public void ShouldRemoveItemWhenTrashClicked()
        {
            // given
            this.cartService.Add(CreateProduct("1", 10m));
            IRenderedComponent<MyCart> renderedPage = Render<MyCart>();

            // when
            renderedPage.Find("button.btn-outline-danger").Click();

            // then
            this.cartService.IsEmpty.Should().BeTrue();
            renderedPage.Markup.Should().Contain("Your cart is empty");
        }

        [Fact]
        public void ShouldClearCartWhenClearClicked()
        {
            // given
            this.cartService.Add(CreateProduct("1", 10m));
            this.cartService.Add(CreateProduct("2", 5m));
            IRenderedComponent<MyCart> renderedPage = Render<MyCart>();

            // when
            renderedPage.Find("button.btn-link.text-danger").Click();

            // then
            this.cartService.IsEmpty.Should().BeTrue();
        }
    }
}
