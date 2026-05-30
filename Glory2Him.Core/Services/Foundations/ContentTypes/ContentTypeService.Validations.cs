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

using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    public partial class ContentTypeService
    {
        private static void ValidateOnAddContentType(ContentType contentType)
        {
            ValidateContentTypeIsNotNull(contentType);
        }

        private static void ValidateContentTypeIsNotNull(ContentType contentType)
        {
            if (contentType is null)
            {
                throw new NullContentTypeException(message: "Content type is null.");
            }
        }
    }
}
