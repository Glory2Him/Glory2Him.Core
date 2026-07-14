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
using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Views.Products.Exceptions;
using Xeptions;

namespace Glory2Him.WebApp.Services.Views.Products
{
    public partial class ProductsViewService
    {
        private delegate ValueTask<T> ReturningProductsFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningProductsFunction<T> returningProductsFunction)
        {
            try
            {
                return await returningProductsFunction();
            }
            catch (Exception exception)
            {
                var failedProductsViewServiceException =
                    new FailedProductsViewServiceException(
                        message: "Failed products view service error occurred, contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedProductsViewServiceException);
            }
        }

        private async ValueTask<ProductsViewServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var productsViewServiceException =
                new ProductsViewServiceException(
                    message: "Products view service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(productsViewServiceException);

            return productsViewServiceException;
        }
    }
}
