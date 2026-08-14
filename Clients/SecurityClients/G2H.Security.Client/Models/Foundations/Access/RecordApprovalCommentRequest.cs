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
    }
}
