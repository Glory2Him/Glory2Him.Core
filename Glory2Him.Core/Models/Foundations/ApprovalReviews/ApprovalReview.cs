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

using System;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Models.Foundations.ApprovalReviews
{
    /// <summary>
    /// Represents an approval review associated with an approval record.
    /// </summary>
    public class ApprovalReview : IKey, IAudit
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
        /// Identifier of the user who made the review.
        /// </summary>
        public string ReviewerId { get; set; } = string.Empty;

        /// <summary>
        /// The status associated with this approval review.
        /// </summary>
        public ApprovalStatus StatusId { get; set; }

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
        /// Navigation to the approval this item belongs to.
        /// </summary>
        public Approval Approval { get; set; } = null!;
    }
}
