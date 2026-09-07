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

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// Everything the "may this actor add a comment to this approval?" decision consults.
    ///
    /// <para>Notice what is <b>absent</b>: no role subjects and no tier. Commenting carries no
    /// review tier — anyone who may contribute may speak on an approval they can see, and the
    /// contribution gate is row-local and stays in the foundation service (§14.6). What this
    /// decision adds is the pair of facts about the <i>parent</i> that a single-entity service
    /// may not read for itself.</para>
    ///
    /// <para>No existing-comment list either. Unlike a review there is no one-per-person rule to
    /// enforce: any number of comments may be added, by anyone, for as long as the round is
    /// open.</para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>: an ungathered fact would default to the
    /// permissive answer.</para>
    /// </summary>
    public class RecordApprovalCommentRequest
    {
        /// <summary>
        /// The user attempting to add the comment.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// The parent approval's current state. A comment may only be added while the round is
        /// open — once the approval reaches <c>Approved</c> or <c>Rejected</c> the thread closes
        /// with it, because a comment added afterwards would neither be read by the workflow nor
        /// re-run it.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Whether the parent approval is soft-deleted.
        ///
        /// <para>Carried as its own fact because the foreign key cannot answer it. Deletion is a
        /// flag and the row stays (§10.4, §9.7.2 rule 2), so <c>ApprovalId</c> still resolves to a
        /// taken-down approval and the key is satisfied by a parent that should accept nothing.
        /// </para>
        /// </summary>
        public required bool IsParentApprovalDeleted { get; init; }

        /// <summary>
        /// Whether the comment being created is an <b>ask</b> — a question or a change request —
        /// rather than a remark.
        ///
        /// <para>A <c>bool</c> rather than a mirrored enum for the reason this whole package takes
        /// strings for entity and content types: the vocabulary belongs to the consuming
        /// application and the project reference runs the other way, so a second copy of
        /// <c>ApprovalCommentType</c> here would be one more thing to keep in step for no decision
        /// it would let this client make.</para>
        /// </summary>
        public required bool IsAsk { get; init; }

        /// <summary>
        /// The resolution the caller is asking to be born with — <c>true</c> for settled.
        ///
        /// <para>Gathered because the pairing of this with <see cref="IsAsk"/> is a gate question,
        /// not a shape question. §7.8 rule 1 leaves the FIELD unconstrained on purpose — both
        /// birth values are legitimate, and pinning it would have made it impossible to leave a
        /// remark without blocking the approval. That reasoning predates
        /// <c>ApprovalCommentType</c>: the add path could not tell an observation from an ask, so
        /// it could not rule on either. Now it can, and exactly one of the four pairings is
        /// refused — see <see cref="AccessDenialReason.SettledAskNotPermitted"/>.</para>
        /// </summary>
        public required bool IsSettled { get; init; }
    }
}
