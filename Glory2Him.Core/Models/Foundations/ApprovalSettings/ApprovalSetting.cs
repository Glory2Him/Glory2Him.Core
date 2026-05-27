// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;

namespace Glory2Him.Core.Models.Foundations.ApprovalSettings
{
    /// <summary>
    /// Defines policy rules for the approval workflow for a specific entity type.
    /// </summary>
    public class ApprovalSetting : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the approval setting.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The entity type this approval setting applies to.
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// Number of approvals required before the entity is considered approved.
        /// </summary>
        public int RequiredApprovals { get; set; }

        /// <summary>
        /// Indicates whether the author of the entity may approve their own submission.
        /// </summary>
        public bool AllowSelfApproval { get; set; }

        /// <summary>
        /// Indicates whether a single rejection immediately blocks the approval.
        /// </summary>
        public bool BlockOnReject { get; set; }

        /// <summary>
        /// Indicates whether edits to the entity reset existing approval reviews.
        /// </summary>
        public bool RequireReapprovalOnChange { get; set; }

        /// <summary>
        /// Indicates whether the entity is automatically approved when the required threshold is met.
        /// </summary>
        public bool AutoApproveIfThresholdMet { get; set; }

        /// <summary>
        /// Indicates whether approval and rejection is restricted to users in a configured role.
        /// </summary>
        public bool MustBeInRoleToApprove { get; set; }

        /// <summary>
        /// User identifier for who created the approval setting.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval setting was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the approval setting.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval setting was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the approval setting is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// User identifier for who deleted the approval setting.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the approval setting was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Roles permitted to approve or reject for this approval setting.
        /// </summary>
        public ICollection<ApprovalSettingRole> ApprovalSettingRoles { get; set; } =
            new List<ApprovalSettingRole>();
    }
}
