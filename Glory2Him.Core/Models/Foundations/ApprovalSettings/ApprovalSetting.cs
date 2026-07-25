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
using System.Collections.Generic;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;

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
        /// When enabled, entity items require a number of approvals before they can be approved.
        /// </summary>
        public bool RequireApprovals { get; set; } = true;

        /// <summary>
        /// Number of approvals required before the entity is considered approved.
        /// </summary>
        public int RequiredNumberOfApprovals { get; set; } = 1;

        /// <summary>
        /// Indicates whether the entity is automatically approved when the required threshold is met.
        /// </summary>
        public bool AutoApproveIfAllApprovalRequirementsMet { get; set; } = false;

        /// <summary>
        /// Indicates whether the author of the entity may approve their own submission.
        /// </summary>
        public bool AllowSelfApproval { get; set; } = false;

        /// <summary>
        /// Indicates whether a single rejection immediately blocks the approval.
        /// </summary>
        public bool BlockOnReject { get; set; } = false;

        /// <summary>
        /// Indicates whether edits to the entity reset existing approval reviews.
        /// </summary>
        public bool RequireReapprovalOnChange { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether all comments must be resolved before approval can be granted.
        /// </summary>
        public bool RequireApprovalCommentResolutionBeforeApproval { get; set; } = true;

        /// <summary>
        /// Indicates whether bypassing approval settings is allowed.
        /// </summary>
        public bool DoNotAllowBypassingSettings { get; set; } = false;

        /// <summary>
        /// Specifies whether the approval setting restricts who can review based on usernames and/or roles.
        /// </summary>
        public bool RestrictWhoCanReview { get; set; } = false;

        /// <summary>
        /// Specifies whether the approval setting restricts who can approve based on usernames and/or roles.
        /// </summary>
        public bool RestrictWhoCanApprove { get; set; } = false;

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
        /// Roles permitted to review for this approval setting.
        /// </summary>
        public ICollection<ApprovalSettingReviewerRole> ApprovalSettingReviewerRoles { get; set; } =
            new List<ApprovalSettingReviewerRole>();

        /// <summary>
        /// Roles permitted to publish for this approval setting.
        /// </summary>
        public ICollection<ApprovalSettingPublisherRole> ApprovalSettingPublisherRoles { get; set; } =
            new List<ApprovalSettingPublisherRole>();
    }
}
