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

namespace Glory2Him.Core.Models.Foundations.Tags
{
    public class Tag : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the tag.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Title of the content item (optional).
        /// </summary>
        public string Name { get; set; }

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
    }
}
