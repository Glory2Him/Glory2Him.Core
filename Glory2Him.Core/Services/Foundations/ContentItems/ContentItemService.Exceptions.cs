// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial class ContentItemService
    {
        private delegate ValueTask<ContentItem> ReturningContentItemFunction();

        private async ValueTask<ContentItem> TryCatch(ReturningContentItemFunction returningContentItemFunction)
        {
            try
            {
                return await returningContentItemFunction();
            }
            catch (NullContentItemException nullContentItemException)
            {
                var contentItemValidationException = new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemException);

                await this.loggingBroker.LogErrorAsync(contentItemValidationException);

                throw contentItemValidationException;
            }
        }
    }
}
