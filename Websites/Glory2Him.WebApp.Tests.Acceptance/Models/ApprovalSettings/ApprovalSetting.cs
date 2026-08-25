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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings
{
    /// <summary>
    /// The wire shape of an approval policy row.
    ///
    /// <para><b>No approval fields</b>, unlike every other model in this project. §7.5 entry 9
    /// makes <c>ApprovalSetting</c> approvable only <i>"if policy changes require approval"</i>,
    /// a conditional never taken up — the entity carries no <c>ApprovalStatus</c>,
    /// <c>IsPublished</c>, <c>PublishDate</c> or bypass pair, so a model copied from a content
    /// entity would declare five properties the API never sends.</para>
    /// </summary>
    public class ApprovalSetting
    {
        public Guid Id { get; set; }

        // The scope. EntityType alone is the per-type DEFAULT; EntityType plus ContentType is an
        // override. Each has its own filtered unique index, so exactly one row can occupy either
        // scope (§8.4 policy resolution depends on that).
        public EntityType EntityType { get; set; }
        public ContentType? ContentType { get; set; }

        // The policy itself (§8.2).
        public bool RequireApprovals { get; set; }
        public int RequiredNumberOfApprovals { get; set; }
        public bool AutoApproveIfAllApprovalRequirementsMet { get; set; }
        public bool AllowSelfApproval { get; set; }
        public bool BlockOnReject { get; set; }
        public bool BlockOnZeroApprovalScore { get; set; }
        public bool RequireReapprovalOnChange { get; set; }
        public bool RequireReviewCommentResolutionBeforeApprovals { get; set; }
        public bool DoNotAllowBypassingSettings { get; set; }

        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedWhen { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset UpdatedWhen { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedWhen { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletionReason { get; set; }
    }
}
