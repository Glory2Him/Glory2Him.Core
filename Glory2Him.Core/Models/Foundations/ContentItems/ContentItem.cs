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

namespace Glory2Him.Core.Models.Foundations.ContentItems
{
    /// <summary>
    /// Represents a versioned content item.
    /// </summary>
    public class ContentItem : IKey, IAudit, IVersion, IApproval
    {
        /// <summary>
        /// Primary key identifier for the content item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The classification of the content item. Set on creation; never accepted from
        /// a caller on modify (design §12.4.1 rule 7a) — different content types carry
        /// different validation rules, so an item cannot be relabelled into a type its
        /// content was never checked against.
        /// </summary>
        public ContentType ContentType { get; set; }

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
        /// Gets or sets the basis on which this content is permitted to be shared.
        /// </summary>
        public ShareabilityBasis ShareabilityBasis { get; set; }

        /// <summary>
        /// Share permission for the content item (optional).
        /// </summary>
        public string? SharePermission { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hash of the normalized <see cref="Content"/> (trimmed, whitespace collapsed,
        /// lowercased). Control field computed on every write; used for duplicate detection
        /// per content type. Never accepted from an external caller.
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        /// <inheritdoc cref="IVersion.GroupId" />
        public Guid GroupId { get; set; }

        /// <summary>
        /// Version number of the content item (required, defaults to 1).
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The date and time when the content item was published. 
        /// This is nullable to allow for drafts that have not yet been published.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content item is published.
        /// </summary>
        public bool IsPublished { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether the content item is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

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
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }
    }
}
