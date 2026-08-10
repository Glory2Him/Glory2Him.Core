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

namespace Glory2Him.Core.Models.Foundations.Tags
{
    public class Tag : IKey, IAudit, IApproval
    {
        /// <summary>
        /// Primary key identifier for the tag.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the tag.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the tag was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the tag.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the tag was last updated.
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
        /// Gets or sets a value indicating whether the content item is published.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// The date and time when the tag was published.
        /// This is nullable to allow for drafts that have not yet been published.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// A denormalized field to indicate if the content item has been approved. 
        /// This is used to optimize queries for approved content items without 
        /// needing to join with the approvals table.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

        /// <summary>
        /// A denormalized field to indicate whether the approval conditions were bypassed to
        /// reach the current status. Derived on write from the access decision, never accepted
        /// from a caller.
        /// </summary>
        public bool IsApprovedByBypass { get; set; } = false;

        /// <summary>
        /// A denormalized field recording why the approval conditions were waived. Populated
        /// only when <see cref="IsApprovedByBypass"/> is true, and derived on write from the
        /// same decision, never accepted from a caller.
        /// </summary>
        public string? ApprovedByBypassReason { get; set; }
    }
}
