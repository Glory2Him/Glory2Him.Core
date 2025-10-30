// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Models.Foundations.ContentTypes
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public class ContentType : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the content type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the content type (e.g., Quote, Story, Testimony).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the content type.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content type was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the content type.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content type was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// Navigation to the content items associated with this content type.
        /// </summary>
        public ICollection<ContentItem> ContentItems { get; set; } = new List<ContentItem>();
    }
}
