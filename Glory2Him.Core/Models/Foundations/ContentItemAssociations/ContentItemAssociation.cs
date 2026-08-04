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

namespace Glory2Him.Core.Models.Foundations.ContentItemAssociations
{
    /// <summary>
    /// Represents a content item association.
    /// </summary>
    public class ContentItemAssociation : IKey, IAudit, IApproval
    {
        /// <summary>
        /// Primary key identifier for the content item.
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        /// Type identifier for the content item. (nullable if AllVersions)
        /// </summary>
        public Guid? LinkedContentItemId { get; set; }

        /// <summary>
        /// The target content item group identifier for the association.
        /// Used when <see cref="Scope"/> is <c>AllVersions</c>; null when scope is <c>ThisVersionOnly</c>.
        /// </summary>
        public Guid? LinkedContentItemGroupId { get; set; }

        /// <summary>
        /// Gets or sets the scope level for the association. AllVersions = 0, ThisVersionOnly = 1.
        /// </summary>
        public Scope LinkedContentScope { get; set; } = Scope.AllVersions;

        /// <summary>
        /// Type entity type identifier.
        /// </summary>
        public EntityType LinkedEntityType { get; set; }

        /// <summary>
        /// The entity identifier.
        /// </summary>
        public Guid LinkedEntityId { get; set; }

        /// <summary>
        /// The confidence score of the association, indicating the strength or reliability
        /// of the link between the content item and the associated entity.
        /// A higher score suggests a stronger association. [0-10]
        /// </summary>
        public int? AssociationConfidenceScore { get; set; }

        /// <summary>
        /// Reason for the confidence level assigned to the association.
        /// </summary>
        public string? AssociationConfidenceReason { get; set; }

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
    }
}
