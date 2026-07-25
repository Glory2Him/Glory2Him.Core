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

namespace Glory2Him.Core.Models.Foundations.BibleReferences
{
    /// <summary>
    /// Represents a scripture reference associated with content through <see cref="ContentItemAssociation"/>.
    /// </summary>
    public class BibleReference : IKey, IAudit, IVersion, IApproval
    {
        /// <summary>
        /// Primary key identifier for the Bible reference.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The scripture reference, such as John 3:16.
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// The Bible translation, such as NIV, KJV, or ESV.
        /// </summary>
        public string Translation { get; set; } = string.Empty;

        /// <summary>
        /// Optional scripture text for the referenced passage.
        /// </summary>
        public string? Scripture { get; set; }

        /// <summary>
        /// User identifier for who created the Bible reference.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the Bible reference was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the Bible reference.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the Bible reference was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the Bible reference.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the Bible reference was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Bible reference is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Content item group identifier that groups all versions of this Bible reference together.
        /// Populated on creation and shared across all versions.
        /// </summary>
        public Guid ContentItemGroupId { get; set; }

        /// <summary>
        /// Version number of this Bible reference record, defaults to 1.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether this is the latest version of the Bible reference.
        /// </summary>
        public bool IsLatestVersion { get; set; } = false;

        /// <summary>
        /// Optional date and time from which the Bible reference becomes visible.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Bible reference is published.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Denormalized approval state mirroring the linked <see cref="Approval"/> record.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    }
}
