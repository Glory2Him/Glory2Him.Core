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

namespace Glory2Him.Core.Models.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// Records that Berean — the AI reviewer of design §8.6.2 — has been assigned to an
    /// approval round, and tracks whether its automated pass has run.
    ///
    /// <para><b>Why this is not an <c>ApprovalReviewRequest</c>.</b> That entity models an
    /// invitation to a specific eligible PERSON — it carries <c>RequestedUserId</c>, matched
    /// against an answering <c>ApprovalReview.CreatedBy</c>, and its uniqueness is scoped per
    /// person per approval. Berean is not a role-bearing human identity: it acts under a system
    /// identity (<c>SecurityContext.IsSystemIdentity</c>), never holds an account, and never
    /// answers through its own <c>ApprovalReview</c> in a way that needs matching back to a
    /// requested user. There is nobody for a second uniqueness dimension to name, so this row
    /// drops it entirely — at most one LIVE assignment exists per approval, full stop.</para>
    ///
    /// <para><b>The two flags are system-only, today.</b> <see cref="IsAIReviewCompleted"/> and
    /// <see cref="IsAIReviewCommentsPresent"/> exist as capabilities a future automated review
    /// process will flip once it runs the classification library and files Berean's
    /// <c>ApprovalComment</c>/<c>ApprovalReview</c> (design §8.6.2, GitHub issue #354). Nothing in
    /// this pass calls that process or ever sets either flag <c>true</c> — <c>Modify</c> exists so
    /// the capability is there when that process is built, not because anything uses it yet.</para>
    /// </summary>
    public class AIReviewerAssignment : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the AI reviewer assignment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the approval round Berean has been assigned to review.
        ///
        /// <para>A plain column, deliberately without a foreign key or navigation property —
        /// unlike its <c>ApprovalReviewRequest</c> sibling. Establishing one here would require
        /// adding a reverse collection to <c>Approval</c>, a model this entity has no other
        /// reason to touch; the filtered unique index below is what actually enforces the
        /// one-live-assignment invariant, and it needs no relationship to do it.</para>
        /// </summary>
        public Guid ApprovalId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Berean's automated review pass has completed
        /// for this assignment.
        ///
        /// <para>Defaults <c>false</c> and is only ever set <c>true</c> by the future system
        /// process that runs the classification library (design §8.6.2) — nothing in this pass
        /// flips it. A human caller may still reach <c>Modify</c>, because the flag is a fact
        /// about work having happened, not a permission this service arbitrates. The add path
        /// refuses a row that arrives with it already set: an assignment is born pending, and a
        /// row claiming a finished pass at the moment it is created claims work that never
        /// ran.</para>
        /// </summary>
        public bool IsAIReviewCompleted { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Berean left one or more <c>ApprovalComment</c>
        /// rows under its system identity as part of this assignment.
        ///
        /// <para>Defaults <c>false</c> for the same reason as <see cref="IsAIReviewCompleted"/>:
        /// it is a fact the future review process records, not one this service computes. It is
        /// refused on add for that same reason, and — on add and on modify alike — cannot stand
        /// <c>true</c> while <see cref="IsAIReviewCompleted"/> is <c>false</c>: it records
        /// something Berean left behind, and a pass that never finished left nothing.</para>
        /// </summary>
        public bool IsAIReviewCommentsPresent { get; set; } = false;

        /// <summary>
        /// User identifier for who created the AI reviewer assignment — the person (or system
        /// process) that assigned Berean to the round.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the AI reviewer assignment was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the AI reviewer assignment.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the AI reviewer assignment was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who removed the AI reviewer assignment.
        /// </summary>
        public string? DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the AI reviewer assignment was removed.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the AI reviewer assignment is removed.
        /// A removed assignment renders nowhere and frees the round's assignment slot, so
        /// Berean can be assigned to it again — a fresh row, because the withdrawn one is
        /// closed to writes and reads alike, both reported as not found (§14.5).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for removal, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }
    }
}
