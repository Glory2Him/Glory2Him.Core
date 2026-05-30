// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    public partial class ContentTypeService
    {
        private delegate ValueTask<ContentType> ReturningContentTypeFunction();

        private async ValueTask<ContentType> TryCatch(ReturningContentTypeFunction returningContentTypeFunction)
        {
            try
            {
                return await returningContentTypeFunction();
            }
            catch (NullContentTypeException nullContentTypeException)
            {
                throw await CreateAndLogValidationException(nullContentTypeException);
            }
            catch (InvalidContentTypeException invalidContentTypeException)
            {
                throw await CreateAndLogValidationException(invalidContentTypeException);
            }
        }

        private async ValueTask<ContentTypeValidationException> CreateAndLogValidationException(Xeption exception)
        {
            var contentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeValidationException);

            return contentTypeValidationException;
        }
    }
}
