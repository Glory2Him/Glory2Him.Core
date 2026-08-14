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
    /// Everything the §8.5 approval-conditions formula consults, and nothing else.
    ///
    /// <para><b>Every property is <c>required</c>, and that is a security property rather than
    /// tidiness.</b> This client is a pure function: it cannot fetch what it was not given, so an
    /// ungathered list does not read as "unknown", it reads as "empty" — and empty is the
    /// permissive answer to both questions asked of one. An ungathered comment list makes "all
    /// comments are resolved" vacuously true; an ungathered review list makes a rejection
    /// invisible. Both fail <i>open</i>, and both would pass every test written against them.
    /// <c>required</c> turns each of those into a compile error at the call site instead.</para>
    /// </summary>
    public class ApprovalConditionsRequest
    {
        /// <summary>
        /// Every policy row that could apply, unresolved. Resolution is part of the decision
        /// (§8.4). Pass an empty list when none exist — the fail-closed system default is used.
        /// </summary>
        public required IReadOnlyList<ApprovalPolicy> CandidatePolicies { get; init; }

        /// <summary>
        /// The entity type half of the policy key — the entity's <b>own</b> type. For an
        /// association this is <c>Association</c>, never an endpoint's type.
        /// </summary>
        public required string EntityType { get; init; }

        /// <summary>
        /// The content type half of the policy key, or null for the entity-type default tier.
        /// </summary>
        public required string? ContentType { get; init; }

        /// <summary>
        /// Every review on the approval, including dismissed and soft-deleted ones. Which count
        /// is decided here (§8.5 rule 3), not by the caller.
        /// </summary>
        public required IReadOnlyList<ReviewRecord> Reviews { get; init; }

        /// <summary>
        /// Every comment on the approval, including soft-deleted ones.
        /// </summary>
        public required IReadOnlyList<ApprovalCommentRecord> ApprovalComments { get; init; }

        /// <summary>
        /// The entity's confidence score, or null.
        ///
        /// <para>Null covers two situations that behave identically and so need no separate flag:
        /// the entity has no confidence concept at all, and the confidence process has not run
        /// yet. Neither blocks. Only an explicit <c>0</c> blocks, and only when the policy says
        /// so — treating null as blocking would deadlock every approval until the scoring process
        /// ships, and would strand anything it failed on (§8.5 rule 8).</para>
        /// </summary>
        public required decimal? ConfidenceScore { get; init; }
    }
}
