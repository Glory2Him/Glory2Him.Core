// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Foundations.ContentTypes;

namespace Glory2Him.Core.Models.Foundations.ContentItems
{
    /// <summary>
    /// Represents a versioned content item.
    /// </summary>
    public class ContentItem : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the content item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type identifier for the content item (e.g., quote, story, testimony).
        /// </summary>
        public Guid ContentTypeId { get; set; }

        /// <summary>
        /// Title of the content item (optional).
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Author of the content item (optional).
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Body content of the item (required).
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Content item group identifier to group multiple versions of the same content item.
        /// </summary>
        public Guid ContentItemGroupId { get; set; }

        /// <summary>
        /// Version number of the content item (required, defaults to 1).
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether the current instance represents the latest version.
        /// </summary>
        public bool IsLatest { get; set; } = false;

        /// <summary>
        /// User identifier for who created the content item.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the content item.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// Navigation to the content type this item belongs to.
        /// </summary>
        public ContentType? ContentType { get; set; }
    }
}
