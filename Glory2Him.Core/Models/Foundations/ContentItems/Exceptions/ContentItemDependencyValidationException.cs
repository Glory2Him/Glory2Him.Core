// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Xeptions;

namespace Glory2Him.Core.Models.Foundations.ContentItems.Exceptions
{
    public class ContentItemDependencyValidationException : Xeption
    {
        public ContentItemDependencyValidationException(string message, Xeption innerException)
            : base(message, innerException)
        { }
    }
}
