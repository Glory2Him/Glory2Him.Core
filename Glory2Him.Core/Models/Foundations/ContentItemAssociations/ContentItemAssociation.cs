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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Foundations.ContentItemAssociations
{
    /// <summary>
    /// Represents a content item association.
    /// </summary>
    public class ContentItemAssociation : IKey, IAudit, IVersion, IApproval
    {
        /// <summary>
        /// Primary key identifier for the content item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the scope level for the association. AllVersions = 0, ThisVersionOnly = 1.
        /// </summary>
        public Scope Scope { get; set; }

        /// <summary>
        /// Type identifier for the content item. (nullable if AllVersions)
        /// </summary>
        public Guid? ContentItemId { get; set; }

        /// <summary>
        /// Content item group Id used to group all versions of the content item. (nullable if ThisVersionOnly)
        /// </summary>
        public Guid? ContentItemGroupId { get; set; }

        /// <summary>
        /// Type entity type identifier.
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// The entity identifier.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The approval identifier.
        /// </summary>
        public Guid ApprovalId { get; set; }

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
    }
}
