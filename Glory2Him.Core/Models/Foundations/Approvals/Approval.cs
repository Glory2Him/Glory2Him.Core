// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Models.Foundations.Approvals
{
    /// <summary>
    /// Represents an approval record tied to a specific entity type and entity id
    /// </summary>
    public class Approval : IKey, IAudit
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Logical type of the approved entity (e.g., "Quote", "Story", "Tag", "Comment", "Reaction").
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// Identifier of the approved entity instance.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The approvals status (maps to ApprovalStatus enum).
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; }

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
        /// User identifier for who deleted the content item.
        /// </summary>
        public string? DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content item is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Navigation to the approval comments associated with this approval.
        /// </summary>
        public ICollection<ApprovalComment> ApprovalComments { get; set; } = new List<ApprovalComment>();

        /// <summary>
        /// Navigation to the approval reviews associated with this approval.
        /// </summary>
        public ICollection<ApprovalReview> ApprovalReviews { get; set; } = new List<ApprovalReview>();
    }
}
