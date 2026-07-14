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

using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Views.Products;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class ProductCardComponent
    {
        [Parameter]
        [EditorRequired]
        public ProductView Product { get; set; } = new ProductView();

        [Parameter]
        public EventCallback<ProductView> OnAddToCart { get; set; }

        private string ProductHref => $"shop-detail/{Product.Slug}";

        private string StarIcon(int position)
        {
            if (Product.Rating >= position)
            {
                return "fa-star";
            }

            return Product.Rating >= position - 0.5 ? "fa-star-half-alt" : "fa-star";
        }

        private async Task AddToCartAsync() =>
            await OnAddToCart.InvokeAsync(Product);
    }
}
