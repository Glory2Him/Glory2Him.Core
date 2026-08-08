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
    /// The answer to "may this actor do this?" — one question, one answer, one place.
    ///
    /// <para>Deliberately not an <c>ApprovalPolicy</c>. Handing back the resolved settings would
    /// mean every calling service re-implemented the decision over them, and seven copies of a
    /// rule are seven chances to drift (§8.6.1 rule 4).</para>
    /// </summary>
    public class AccessVerdict
    {
        /// <summary>
        /// Whether the act is permitted. This is the whole answer; nothing else on this object
        /// needs to be consulted to enforce it.
        /// </summary>
        public required bool IsPermitted { get; init; }

        /// <summary>
        /// Why it was refused, or <see cref="AccessDenialReason.None"/> when permitted. Switch on
        /// this to author a caller-facing message.
        /// </summary>
        public required AccessDenialReason DenialReason { get; init; }

        /// <summary>
        /// A human-readable account of the decision, for the server-side log.
        ///
        /// <para><b>Never put this in an exception message or an exception's <c>Data</c>.</b> Both
        /// surface outward to callers (§14.5 rule 2), and this string is composed from resolved
        /// policy values — the required approval count, which block fired — so echoing it hands an
        /// unprivileged caller the approval configuration through a public event address. Log it
        /// immediately before throwing, and throw wording of the caller's own (§14.5's
        /// closing rule).</para>
        /// </summary>
        public required string Explanation { get; init; }
    }
}
