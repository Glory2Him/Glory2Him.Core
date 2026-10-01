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

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// One review still active on a round, projected for the changed-reaction flow (§APR9.7.4):
    /// when it was written, so the flow can tell the old pair's reviews from the new pair's, and
    /// whether it rejects.
    ///
    /// <para>Gathered UNFILTERED by the access broker, for the reason
    /// FindDismissableApprovalReviewIdsAsync already carries: the reader who changed their
    /// reaction may see none of the round's reviews, and an identity-filtered read never decides
    /// an invariant.</para>
    /// </summary>
    public class DismissableApprovalReview
    {
        /// <summary>The review row, so the flow can dismiss it.</summary>
        public required Guid Id { get; init; }

        /// <summary>When the review was written, compared with the reaction change's own
        /// moment.</summary>
        public required DateTimeOffset CreatedWhen { get; init; }

        /// <summary>Whether the review's status is <c>Rejected</c>.</summary>
        public required bool IsRejection { get; init; }
    }
}
