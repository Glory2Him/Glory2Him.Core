// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;

namespace Glory2Him.Core.Models.Foundations.Approvals
{
    /// <summary>
    /// Represents an approval record tied to a specific entity type and entity id
    /// </summary>
    public class Approval
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Logical type of the approved entity (e.g., "Quote", "Story", "Tag", "Comment", "Reaction").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the approved entity instance.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Numeric status identifier (map to an enum in your domain layer if desired).
        /// </summary>
        public int StatusId { get; set; }

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
