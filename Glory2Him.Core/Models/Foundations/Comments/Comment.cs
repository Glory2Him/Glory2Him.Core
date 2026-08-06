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

namespace Glory2Him.Core.Models.Foundations.Comments
{
    /// <summary>
    /// Represents a user comment associated with a content item through <see cref="Association"/>.
    /// </summary>
    public class Comment : IKey, IAudit, IApproval
    {
        /// <summary>
        /// Primary key identifier for the comment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The body text of the comment.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the comment.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the comment was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the comment.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the comment was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the comment.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the comment was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the comment is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the comment is published.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Optional date and time from which the comment becomes visible.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Denormalized approval state mirroring the linked <see cref="Approval"/> record.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    }
}
