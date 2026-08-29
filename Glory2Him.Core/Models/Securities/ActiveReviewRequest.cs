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
    /// One invitation still outstanding on an approval, projected for the invitation flow
    /// (design 7.9).
    ///
    /// <para>Gathered UNFILTERED by the access broker, and that is the point. The caller-facing
    /// read on ApprovalReviewRequestService applies a visibility filter, and an identity-filtered
    /// read must never decide an invariant: a moderator who cannot see somebody else's invitation
    /// would be told the person is invitable and would then collide with the uniqueness index.
    /// The same reasoning FindDismissableApprovalReviewIdsAsync already carries.</para>
    /// </summary>
    public class ActiveReviewRequest
    {
        /// <summary>The request row, so rule 5 can withdraw it and rule 6 can retire it.</summary>
        public required Guid Id { get; init; }

        /// <summary>Who was invited - what rule 4 matches on to dissolve a duplicate, and what a
        /// surface groups its Requested section by.</summary>
        public required string RequestedUserId { get; init; }
    }
}
