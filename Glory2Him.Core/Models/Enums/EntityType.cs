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

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// Defines the distinct entity types that can be associated
    /// with a <see cref="ContentItem"/> through <see cref="Association"/>.
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
        Attachment = 6,

        /// <summary>
        /// An association record that itself participates in the approval workflow.
        /// </summary>
        Association = 7
    }
}
