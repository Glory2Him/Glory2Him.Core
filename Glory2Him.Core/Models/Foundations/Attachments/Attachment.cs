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

namespace Glory2Him.Core.Models.Foundations.Attachments
{
    /// <summary>
    /// Represents a file or binary resource associated with content through <see cref="Association"/>.
    /// </summary>
    public class Attachment : IKey, IAudit, IVersion, IApproval
    {
        /// <summary>
        /// Primary key identifier for the attachment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name for the attachment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Storage location URI for the attachment blob.
        /// </summary>
        public string BlobUri { get; set; } = string.Empty;

        /// <summary>
        /// File hash used for integrity verification and deduplication.
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the attachment.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the attachment was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the attachment.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the attachment was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the attachment.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the attachment was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attachment is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Content item group identifier that groups all versions of this attachment together.
        /// Populated on creation and shared across all versions.
        /// </summary>
        public Guid ContentItemGroupId { get; set; }

        /// <summary>
        /// Version number of this attachment record, defaults to 1.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether this is the latest version of the attachment.
        /// </summary>
        public bool IsLatestVersion { get; set; } = false;

        /// <summary>
        /// Optional date and time from which the attachment becomes visible.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attachment is published.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Denormalized approval state mirroring the linked <see cref="Approval"/> record.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

        /// <summary>
        /// Denormalized from the approval record: whether the approval conditions were bypassed
        /// to reach the current status. Derived on write, never accepted from a caller.
        /// </summary>
        public bool IsApprovedByBypass { get; set; } = false;

        /// <summary>
        /// Denormalized from the approval record: why the conditions were waived. Set only when
        /// <see cref="IsApprovedByBypass"/> is true, derived on write, never accepted from a caller.
        /// </summary>
        public string? ApprovedByBypassReason { get; set; }
    }
}
