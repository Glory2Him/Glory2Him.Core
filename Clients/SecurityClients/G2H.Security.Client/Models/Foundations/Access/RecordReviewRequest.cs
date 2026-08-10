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
    /// Everything the "may this actor record a review?" decision consults.
    ///
    /// <para>Notice what is <b>absent</b>: no policy rows. Nothing about recording a review is
    /// configurable — HR-1 admits no setting, the review tier is composed rather than configured
    /// (§8.3), and the window is a fact about the approval's state. Asking for candidate policies
    /// here would have implied a knob that does not exist.</para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>: an ungathered review list makes "this actor has no
    /// active review" vacuously true, which is the permissive answer.</para>
    /// </summary>
    public class RecordReviewRequest
    {
        /// <summary>
        /// The user attempting to record the review.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// Every subject the actor could be authorised through — one for most entities, two for
        /// an association's endpoints. Holding a review-tier role for any one is enough.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }

        /// <summary>
        /// The <c>CreatedBy</c> of the content being reviewed — the entity's author, not the
        /// approval record's.
        ///
        /// <para>This is what HR-1 compares the actor against, so it must be the author of the
        /// <i>content</i>. The approval row's own <c>CreatedBy</c> is the submitter, which is
        /// usually the same person and occasionally is not; using it would make HR-1 quietly
        /// wrong in exactly the cases worth catching.</para>
        /// </summary>
        public required string EntityCreatedBy { get; init; }

        /// <summary>
        /// The parent approval's current state. A review may only be written while it is
        /// <c>Submitted</c> (§7.7 rule 2b) — once the round has closed, a verdict changed
        /// afterwards would not re-run the workflow, and the entity could sit <c>Approved</c> with
        /// a standing rejection against it that nothing notices.
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Every review already on the approval, including dismissed and soft-deleted ones, so
        /// the one-active-review-per-reviewer rule can be applied here (§7.7 rules 1 and 7).
        /// </summary>
        public required IReadOnlyList<ReviewRecord> ExistingReviews { get; init; }

        /// <summary>
        /// True when the actor is amending a review they already hold, rather than filing a new
        /// one. An amendment is expected to find its own active review and must not be refused
        /// for it — which is the whole point of §7.7 rule 1 barring <i>a second</i> active review
        /// rather than barring writes outright.
        /// </summary>
        public required bool IsAmendingOwnReview { get; init; }
    }
}
