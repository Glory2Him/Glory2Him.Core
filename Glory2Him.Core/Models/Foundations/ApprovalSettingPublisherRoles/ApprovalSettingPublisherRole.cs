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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;

namespace Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles
{
    /// <summary>
    /// Defines a role that is permitted to publish for a given <see cref="ApprovalSetting"/>.
    /// </summary>
    public class ApprovalSettingPublisherRole : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the approval setting role.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The parent approval setting this role entry belongs to.
        /// </summary>
        public Guid ApprovalSettingId { get; set; }

        /// <summary>
        /// The name of the role permitted to publish.
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the approval setting role.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval setting role was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the approval setting role.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the approval setting role was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the approval setting role is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// User identifier for who deleted the approval setting role.
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the approval setting role was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Navigation property to the parent approval setting.
        /// </summary>
        public ApprovalSetting ApprovalSetting { get; set; } = null!;
    }
}
