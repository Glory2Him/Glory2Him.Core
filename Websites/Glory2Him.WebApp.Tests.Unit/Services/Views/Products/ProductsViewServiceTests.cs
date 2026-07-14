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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Views.Products;
using Glory2Him.WebApp.Services.Views.Products;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Products
{
    public class ProductsViewServiceTests
    {
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IProductsViewService productsViewService;

        public ProductsViewServiceTests()
        {
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.productsViewService =
                new ProductsViewService(loggingBroker: this.loggingBrokerMock.Object);
        }

        [Fact]
        public async Task ShouldRetrieveAllProducts()
        {
            // given . when
            List<ProductView> actualProducts =
                await this.productsViewService.RetrieveAllProductsAsync();

            // then
            actualProducts.Should().NotBeNullOrEmpty();
            actualProducts.Should().OnlyContain(product => product.Price > 0);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveProductByMatchingSlug()
        {
            // given
            List<ProductView> allProducts =
                await this.productsViewService.RetrieveAllProductsAsync();

            ProductView expectedProduct = allProducts[2];

            // when
            ProductView actualProduct =
                await this.productsViewService.RetrieveProductBySlugAsync(expectedProduct.Slug);

            // then
            actualProduct.Slug.Should().Be(expectedProduct.Slug);
            actualProduct.Name.Should().Be(expectedProduct.Name);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
