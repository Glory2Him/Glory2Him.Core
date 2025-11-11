// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// Defines the distinct entity types that can be associated
    /// with a <see cref="ContentItem"/> through <see cref="ContentItemAssociation"/>.
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// A content item (used for related content, parent/child, or duplication links).
        /// </summary>
        ContentItem = 0,

        /// <summary>
        /// A tag used for categorization or labeling of content items.
        /// </summary>
        Tag = 1,

        /// <summary>
        /// A reaction (e.g., like, love, celebrate) applied to a content item.
        /// </summary>
        Reaction = 2,

        /// <summary>
        /// A Bible reference linked to the content item (e.g., scripture citation).
        /// </summary>
        BibleReference = 3,

        /// <summary>
        /// A comment posted on or about the content item.
        /// </summary>
        Comment = 4,

        /// <summary>
        /// A hyperlink or reference to an external or internal resource.
        /// </summary>
        Link = 5,

        /// <summary>
        /// A file or other binary attachment linked to the content item.
        /// </summary>
        Attachment = 6
    }
}
