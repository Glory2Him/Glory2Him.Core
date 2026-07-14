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
using Glory2Him.WebApp.Models.Views.Products;

namespace Glory2Him.WebApp.Services.Cart
{
    public sealed class CartService : ICartService
    {
        private readonly List<CartItem> items = new();

        public event Action? OnChanged;

        public IReadOnlyList<CartItem> Items => this.items;

        public int Count => this.items.Sum(item => item.Quantity);

        public decimal Subtotal => this.items.Sum(item => item.LineTotal);

        public bool IsEmpty => this.items.Count == 0;

        public void Add(ProductView product, int quantity = 1)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            CartItem? existing =
                this.items.FirstOrDefault(item => item.Product.Id == product.Id);

            if (existing is null)
            {
                this.items.Add(new CartItem { Product = product, Quantity = quantity });
            }
            else
            {
                existing.Quantity += quantity;
            }

            NotifyChanged();
        }

        public void UpdateQuantity(string productId, int quantity)
        {
            CartItem? existing =
                this.items.FirstOrDefault(item => item.Product.Id == productId);

            if (existing is null)
            {
                return;
            }

            if (quantity < 1)
            {
                this.items.Remove(existing);
            }
            else
            {
                existing.Quantity = quantity;
            }

            NotifyChanged();
        }

        public void Remove(string productId)
        {
            this.items.RemoveAll(item => item.Product.Id == productId);
            NotifyChanged();
        }

        public void Clear()
        {
            this.items.Clear();
            NotifyChanged();
        }

        private void NotifyChanged() => OnChanged?.Invoke();
    }
}
