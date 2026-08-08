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
    /// The user a decision is being made about. Everything the decision consults about identity
    /// is here — the client never resolves an identity itself, because there is no HTTP context
    /// on the event path and two identity sources would disagree precisely on the unauthenticated
    /// path (design §8.6.1 rule 5).
    /// </summary>
    public class AccessActor
    {
        /// <summary>
        /// The acting user's identifier.
        ///
        /// <para>This <b>must</b> be resolved by the same function that stamped the row's
        /// <c>CreatedBy</c>. The self-review and self-approval rules compare this value against
        /// <c>CreatedBy</c>, and two different resolvers make that comparison meaningless — it
        /// would silently answer "not the author" for the author.</para>
        /// </summary>
        public required string UserId { get; init; }

        /// <summary>
        /// Every role the actor holds, global and granular alike, exactly as issued. Eligibility
        /// is decided by composing the expected name and looking for it here, so these are
        /// compared verbatim and are case-sensitive.
        /// </summary>
        public required IReadOnlyList<string> Roles { get; init; }

        /// <summary>
        /// Whether the actor is authenticated. An unauthenticated caller is refused every
        /// decision this client makes, before any other rule is consulted.
        /// </summary>
        public required bool IsAuthenticated { get; init; }
    }
}
