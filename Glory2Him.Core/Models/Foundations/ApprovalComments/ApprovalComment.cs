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
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Models.Foundations.ApprovalComments
{
    /// <summary>
    /// Represents an approval comment associated with an approval record.
    /// </summary>
    public class ApprovalComment : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the approval comment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the approval record this comment belongs to.
        /// </summary>
        public Guid ApprovalId { get; set; }

        /// <summary>
        /// Text content of the comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Whether this comment is <b>settled</b> — whether it still requires something before
        /// the approval can proceed.
        ///
        /// <para><b>Not every comment asks for anything.</b> An observation, or a reviewer
        /// recording their rationale so others can see the thinking behind a verdict, is
        /// informational: others may act on it or not, and nothing waits on it. Such a comment is
        /// created <c>IsResolved = true</c> and never blocks. A comment that <i>does</i> ask for
        /// something — a question, a change request — is created <c>false</c> and holds the
        /// approval shut until it is settled.</para>
        ///
        /// <para>So both birth values are legitimate, and the add path deliberately applies no
        /// rule to this field. The column defaults to <c>false</c>, which is the fail-closed
        /// choice for a caller who says nothing.</para>
        ///
        /// <para>Read by <c>ApprovalSetting.RequireReviewCommentResolutionBeforeApprovals</c>:
        /// when that setting is enabled, no outstanding comment may remain before the approval
        /// conditions are met. It gates the <c>Approval</c> entity only — it never affects an
        /// individual <c>ApprovalReview</c>'s own verdict (design §8.5 rule 7).</para>
        ///
        /// <para>Distinct from <c>ApprovalReview.Comment</c>, which is one reviewer's rationale
        /// for their <i>own</i> verdict and is never resolvable at all. A reviewer who wants to
        /// put reasoning in front of the other reviewers writes an <c>ApprovalComment</c> — that
        /// is exactly the informational case above.</para>
        /// </summary>
        public bool IsResolved { get; set; } = false;

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
        /// Navigation to the approval this item belongs to.
        /// </summary>
        public Approval Approval { get; set; } = null!;
    }
}
