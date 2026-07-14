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

using FluentAssertions;
using Glory2Him.WebApp.Models.Views.Products;
using Glory2Him.WebApp.Services.Cart;

namespace Glory2Him.WebApp.Tests.Unit.Services.Cart
{
    public class CartServiceTests
    {
        private readonly ICartService cartService;

        public CartServiceTests() =>
            this.cartService = new CartService();

        private static ProductView CreateProduct(string id, decimal price) =>
            new ProductView { Id = id, Name = $"Product {id}", Price = price };

        [Fact]
        public void ShouldStartEmpty()
        {
            // given . when . then
            this.cartService.IsEmpty.Should().BeTrue();
            this.cartService.Count.Should().Be(0);
            this.cartService.Subtotal.Should().Be(0m);
        }

        [Fact]
        public void ShouldAddProductAndRaiseOnChanged()
        {
            // given
            bool changed = false;
            this.cartService.OnChanged += () => changed = true;

            // when
            this.cartService.Add(CreateProduct("1", 10m), quantity: 2);

            // then
            this.cartService.Count.Should().Be(2);
            this.cartService.Subtotal.Should().Be(20m);
            changed.Should().BeTrue();
        }

        [Fact]
        public void ShouldIncrementQuantityWhenSameProductAddedTwice()
        {
            // given
            ProductView product = CreateProduct("1", 5m);

            // when
            this.cartService.Add(product);
            this.cartService.Add(product, quantity: 3);

            // then
            this.cartService.Items.Should().HaveCount(1);
            this.cartService.Count.Should().Be(4);
        }

        [Fact]
        public void ShouldRemoveItemWhenQuantityUpdatedBelowOne()
        {
            // given
            this.cartService.Add(CreateProduct("1", 5m));

            // when
            this.cartService.UpdateQuantity("1", 0);

            // then
            this.cartService.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void ShouldRemoveProduct()
        {
            // given
            this.cartService.Add(CreateProduct("1", 5m));
            this.cartService.Add(CreateProduct("2", 8m));

            // when
            this.cartService.Remove("1");

            // then
            this.cartService.Items.Should().ContainSingle(item => item.Product.Id == "2");
        }

        [Fact]
        public void ShouldClearAllItems()
        {
            // given
            this.cartService.Add(CreateProduct("1", 5m));
            this.cartService.Add(CreateProduct("2", 8m));

            // when
            this.cartService.Clear();

            // then
            this.cartService.IsEmpty.Should().BeTrue();
        }
    }
}
