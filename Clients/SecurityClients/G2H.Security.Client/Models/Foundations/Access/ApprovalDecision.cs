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
    /// Which way a decider is trying to move an approval.
    ///
    /// <para>The two are not symmetric and must not be collapsed into one "may decide" question.
    /// Approving requires the §8.5 conditions to be met and can be waived only by a recorded
    /// bypass; <b>rejecting requires neither</b>. Rejection withholds approval rather than
    /// granting it, so nothing is being waived — <c>DoNotAllowBypassingSettings</c> does not gate
    /// it and no bypass is recorded (§9.7.5). Asking one question for both would have forced a
    /// rejection to satisfy the approval threshold, which would leave a publisher unable to
    /// reject the very content the threshold was failing to approve.</para>
    /// </summary>
    public enum ApprovalDecision
    {
        /// <summary>Move the approval to <c>Approved</c>.</summary>
        Approve = 0,

        /// <summary>Move the approval to <c>Rejected</c>.</summary>
        Reject = 1,
    }
}
