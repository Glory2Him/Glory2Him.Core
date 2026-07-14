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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Shop;
using Glory2Him.WebApp.Models.Views.Products;
using Glory2Him.WebApp.Models.Views.Products.Exceptions;
using Glory2Him.WebApp.Services.Cart;
using Glory2Him.WebApp.Services.Views.Products;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Shop
{
    public class ShopGridComponentTests : BunitContext
    {
        private readonly Mock<IProductsViewService> productsViewServiceMock;
        private readonly ICartService cartService;

        public ShopGridComponentTests()
        {
            this.productsViewServiceMock = new Mock<IProductsViewService>();
            this.cartService = new CartService();

            Services.AddSingleton(this.productsViewServiceMock.Object);
            Services.AddSingleton(this.cartService);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<ProductView> CreateProducts(int count) =>
            Enumerable.Range(0, count).Select(index => new ProductView
            {
                Id = index.ToString(),
                Name = GetRandomString(),
                Slug = GetRandomString(),
                ImageUrl = "assets/images/shop/01.png",
                Price = 10m + index,
                Rating = 4,
            }).ToList();

        [Fact]
        public void ShouldRenderProductsWhenLoaded()
        {
            // given
            List<ProductView> products = CreateProducts(count: 3);

            this.productsViewServiceMock.Setup(service =>
                service.RetrieveAllProductsAsync())
                    .ReturnsAsync(products);

            // when
            IRenderedComponent<ShopGrid> renderedPage = Render<ShopGrid>();

            // then
            foreach (ProductView product in products)
            {
                renderedPage.Markup.Should().Contain(product.Name);
            }
        }

        [Fact]
        public void ShouldAddProductToCartWhenAddClicked()
        {
            // given
            List<ProductView> products = CreateProducts(count: 1);

            this.productsViewServiceMock.Setup(service =>
                service.RetrieveAllProductsAsync())
                    .ReturnsAsync(products);

            IRenderedComponent<ShopGrid> renderedPage = Render<ShopGrid>();

            // when
            renderedPage.Find("div.card-footer button").Click();

            // then
            this.cartService.Count.Should().Be(1);
            renderedPage.Markup.Should().Contain("Cart (1)");
        }

        [Fact]
        public void ShouldRenderErrorAlertWhenServiceThrows()
        {
            // given
            var serviceException =
                new ProductsViewServiceException("error", new Xeption());

            this.productsViewServiceMock.Setup(service =>
                service.RetrieveAllProductsAsync())
                    .ThrowsAsync(serviceException);

            // when
            IRenderedComponent<ShopGrid> renderedPage = Render<ShopGrid>();

            // then
            renderedPage.Find("div.alert-danger").Should().NotBeNull();
        }
    }
}
