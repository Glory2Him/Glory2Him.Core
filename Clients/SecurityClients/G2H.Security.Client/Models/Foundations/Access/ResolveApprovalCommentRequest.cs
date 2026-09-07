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

using System.Collections.Generic;

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// Everything the "may this actor mark this comment resolved or unresolved?" decision
    /// consults.
    ///
    /// <para>Separate from <see cref="AmendApprovalCommentRequest"/> because it is a separate operation
    /// with a narrower field scope — it owns <c>IsResolved</c> and nothing else (§9.7.1 rule 3,
    /// §10.2 rule 7, the same reason <c>Submit</c>, <c>Approve</c> and <c>Dismiss</c> exist).
    /// Splitting it is what keeps the amend gate owner-only while still letting an administrator
    /// clear a resolution flag, without a "which fields may I touch here" branch inside amend.
    /// </para>
    ///
    /// <para>This is the one comment operation somebody other than the author may perform on the
    /// row, and it is deliberately the only one: resolving records that a comment is settled —
    /// that it no longer requires anything before the approval can proceed — which changes no
    /// words. <c>UpdatedBy</c> then carries the intervener's identity, so it is visible rather
    /// than silent, and the owner may set it back while the round is open.</para>
    ///
    /// <para><b>The tier admitted beside the author is the PUBLISHER tier, not the review tier.</b>
    /// An outstanding comment holds the <i>approval</i> shut under
    /// <c>RequireReviewCommentResolutionBeforeApprovals</c>, and the people that block stops are
    /// exactly the people who decide the approval. A reviewer casts a verdict and is not held by
    /// the gate, so admitting them here would hand the settling of somebody else's ask to somebody
    /// the ask never blocked; a reviewer who wants to answer one writes a comment of their own.
    /// </para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>.</para>
    /// </summary>
    public class ResolveApprovalCommentRequest
    {
        /// <summary>
        /// The user attempting to change the resolution flag.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// Every subject the publisher tier and the <c>ReadOnly</c> veto may be composed from
        /// (§18.6). Usually one; an association names both its endpoints, so a publisher trusted
        /// with either end qualifies.
        ///
        /// <para><b>This is also what finally brings the veto to <c>IsResolved</c>.</b> §18.6
        /// rule 3 records that a scoped block does not reach the comment thread — a comment is
        /// speech about the content, not a write to it — and singles out this one field as the
        /// place that reasoning strains, because settling a comment clears a §8.5 gate. Carrying
        /// the subjects is the closure that note names: the veto is asked here, first, ahead of
        /// the author branch, because a sanction outranks every grant including the holder's own
        /// rows (§18.6 rule 2).</para>
        ///
        /// <para>Carrying the subjects rather than a finished list of role names keeps the naming
        /// convention in one place — the caller composes, this only reports what to compose from.
        /// </para>
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }

        /// <summary>
        /// The <c>CreatedBy</c> of the comment being acted on.
        ///
        /// <para><b>The caller must read this from storage, never from the request payload.</b>
        /// This client is a pure function and cannot fetch it — a payload-supplied value would
        /// let a caller nominate themselves as the author of someone else's row and pass the
        /// ownership gate. <c>AccessBroker</c> passes it through rather than reading it, so the
        /// obligation lands on the <b>foundation service</b>, which already loads the stored
        /// comment before it asks. Nothing in this project or the broker can enforce it.</para>
        /// </summary>
        public required string CommentCreatedBy { get; init; }

        /// <summary>
        /// The parent approval's current state. Resolution only means anything while the round is
        /// open — once the approval has closed, the block this flag feeds has already been
        /// evaluated for the last time.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Whether the parent approval is soft-deleted.
        ///
        /// <para>Asked here for the same reason it is asked on
        /// <see cref="RecordApprovalCommentRequest"/>: the foreign key cannot answer it, because
        /// deletion is a flag and the row stays (§10.4). A taken-down approval accepts no new
        /// comments, and its existing ones stop being resolved with it — otherwise a
        /// comment thread would go on living under an approval that no longer exists to anyone.
        /// </para>
        /// </summary>
        public required bool IsParentApprovalDeleted { get; init; }
    }
}
