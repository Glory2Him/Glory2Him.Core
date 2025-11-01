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
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Models.Foundations.ApprovalComments
{
    /// <summary>
    /// Represents an approval comment associated with an approval record.
    /// </summary>
    public class ApprovalComment : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the approval comment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the approval record this comment belongs to.
        /// </summary>
        public Guid ApprovalId { get; set; }

        /// <summary>
        /// Identifier of the user who made the comment.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Text content of the comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

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
        /// Navigation to the approval this item belongs to.
        /// </summary>
        public Approval Approval { get; set; } = null!;
    }
}
