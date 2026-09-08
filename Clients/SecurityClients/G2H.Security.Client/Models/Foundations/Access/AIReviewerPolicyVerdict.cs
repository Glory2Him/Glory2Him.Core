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
    /// Whether Berean is offered on a round (design §8.6.2) — the ONE field the invitation flow
    /// needs from the resolved <c>ApprovalSetting</c>, answered rather than handed the setting
    /// itself (verdicts are answers, never settings — see <c>IAccessClient</c>'s own doc).
    ///
    /// <para>Deliberately narrower than <c>ApprovalSetting.IsAIAllowedToVote</c> and the two
    /// confidence thresholds: nothing that resolves this verdict casts a vote or reads a score,
    /// so there is nothing yet to ask those three fields.</para>
    /// </summary>
    public class AIReviewerPolicyVerdict
    {
        /// <summary>
        /// The resolved <c>IsAIReviewerOffered</c> — whether Berean should be offered in the
        /// reviewer-request picker and may be assigned at all.
        /// </summary>
        public required bool IsOffered { get; init; }
    }
}
