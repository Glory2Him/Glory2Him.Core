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

namespace Glory2Him.Core.Models.Foundations.Links
{
    /// <summary>
    /// Represents an external or internal link associated with content through <see cref="ContentItemAssociation"/>.
    /// </summary>
    public class Link : IKey, IAudit, IVersion, IApproval
    {
        /// <summary>
        /// Primary key identifier for the link.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name for the link.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Target URL for the link.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Type of the link, such as internal, external, video, article, or source.
        /// </summary>
        public string LinkType { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the link.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the link was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the link.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the link was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the link.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the link was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the link is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Content item group identifier that groups all versions of this link together.
        /// Populated on creation and shared across all versions.
        /// </summary>
        public Guid ContentItemGroupId { get; set; }

        /// <summary>
        /// Version number of this link record, defaults to 1.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether this is the latest version of the link.
        /// </summary>
        public bool G2HatestVersion { get; set; } = false;

        /// <summary>
        /// Optional date and time from which the link becomes visible.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the link is published.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Denormalized approval state mirroring the linked <see cref="Approval"/> record.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    }
}
