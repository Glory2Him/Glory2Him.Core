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

using Glory2Him.WebApp.Models.Views.Products;

namespace Glory2Him.WebApp.Services.Cart
{
    // A demo, in-memory shopping cart scoped to the user's circuit. Not persisted — it stands in
    // for a real cart/order domain in the shop demo.
    public interface ICartService
    {
        event Action? OnChanged;

        IReadOnlyList<CartItem> Items { get; }
        int Count { get; }
        decimal Subtotal { get; }
        bool IsEmpty { get; }

        void Add(ProductView product, int quantity = 1);
        void UpdateQuantity(string productId, int quantity);
        void Remove(string productId);
        void Clear();
    }
}
