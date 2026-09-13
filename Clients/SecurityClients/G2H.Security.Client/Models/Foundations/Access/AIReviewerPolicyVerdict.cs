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
    /// The two composed answers §8.6.2 defines about Berean on a round, from the resolved
    /// <c>ApprovalSetting</c> — answered rather than handed the setting itself (verdicts are
    /// answers, never settings — see <c>IAccessClient</c>'s own doc). Two callers read it: the
    /// invitation flow reads <see cref="IsOffered"/>, and the automatic assignment of §8.6.2.1 —
    /// not an invitation flow at all — reads <see cref="IsAutomaticallyRequested"/>.
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

        /// <summary>
        /// Whether Berean is assigned to a round without anybody asking, once that round enters
        /// review (§8.6.2.1) — composed here as <c>IsOffered &amp;&amp;
        /// IsAIReviewerAutomaticallyRequested</c>, and nowhere else (§8.6.1 rule 4). Storage
        /// guarantees no pairing between the two switches behind this answer — unlike
        /// <c>IsAIAllowedToVote</c>, which a CHECK constraint pairs to the offer — so this field
        /// is what makes the pairing true rather than merely intended.
        /// </summary>
        public required bool IsAutomaticallyRequested { get; init; }
    }
}
