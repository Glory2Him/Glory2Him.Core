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
using Glory2Him.WebApp.Services.Views.Products;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). The shop is a public, read-only demo, so both
    // endpoints stay anonymous — exactly like the Blazor ShopGrid / ShopDetail pages.
    public static class ProductApiEndpoints
    {
        public static IEndpointRouteBuilder MapProductApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder productsGroup = endpoints.MapGroup("/api/products");

            productsGroup.MapGet("/", async (IProductsViewService productsViewService) =>
            {
                List<ProductView> products = await productsViewService.RetrieveAllProductsAsync();

                return Results.Ok(products);
            });

            productsGroup.MapGet("/slug/{slug}", async (
                string slug,
                IProductsViewService productsViewService) =>
            {
                try
                {
                    ProductView product = await productsViewService.RetrieveProductBySlugAsync(slug);

                    return Results.Ok(product);
                }
                catch
                {
                    return Results.NotFound();
                }
            });

            return endpoints;
        }
    }
}
