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
    /// Everything the "may this actor dismiss a review?" decision consults — which is only who is
    /// asking, and what the approval is about.
    ///
    /// <para><b>Notice what is absent, because both omissions are load-bearing.</b></para>
    ///
    /// <para>No <c>ApprovalState</c>. Dismissal is the workflow's own instrument: §8.8 has an
    /// amendment to the reviewed content dismiss every active verdict on it, and that happens
    /// precisely when the round is being re-opened. Admitting the round window here would make
    /// the decision refuse in the one case it exists to serve.</para>
    ///
    /// <para>No <c>ExistingReviews</c>, and no review author. Whether the target is already
    /// dismissed or soft-deleted is row-local, and §7.7 rule 2b keeps row-local checks in the
    /// service. Authorship is deliberately not consulted at all: a verdict belongs to whoever
    /// filed it, and dismissal is something that happens <i>to</i> a review rather than a
    /// retraction its author may perform (§7.7 rule 2, and the ruling recorded on #226).</para>
    ///
    /// <para>What is left is the question a single-entity service cannot answer for itself: is
    /// this actor in the publisher tier <i>for the entity behind this approval</i>. For an
    /// association that means either of its endpoints (§14.7 posture A′ rule 2), which is why
    /// the subjects arrive as a list rather than a single pair.</para>
    /// </summary>
    public class DismissReviewRequest
    {
        /// <summary>
        /// The user attempting to dismiss the review.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// Every subject the actor could be authorised through — one for most entities, two for
        /// an association's endpoints. Holding a publisher-tier role for any one is enough.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }
    }
}
