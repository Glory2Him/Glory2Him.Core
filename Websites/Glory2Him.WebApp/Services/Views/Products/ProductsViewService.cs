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
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Views.Products;

namespace Glory2Him.WebApp.Services.Views.Products
{
    public partial class ProductsViewService : IProductsViewService
    {
        private readonly ILoggingBroker loggingBroker;

        public ProductsViewService(ILoggingBroker loggingBroker) =>
            this.loggingBroker = loggingBroker;

        public ValueTask<List<ProductView>> RetrieveAllProductsAsync() =>
            TryCatch(() => new ValueTask<List<ProductView>>(SampleProducts.All));

        public ValueTask<ProductView> RetrieveProductBySlugAsync(string slug) =>
            TryCatch(() =>
            {
                ProductView product =
                    SampleProducts.All.FirstOrDefault(product =>
                        string.Equals(product.Slug, slug, StringComparison.OrdinalIgnoreCase))
                            ?? SampleProducts.All.First();

                return new ValueTask<ProductView>(product);
            });
    }
}
