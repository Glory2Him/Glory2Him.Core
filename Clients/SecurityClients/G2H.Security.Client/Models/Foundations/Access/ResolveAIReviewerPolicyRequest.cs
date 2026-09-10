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
    /// The policy key alone (design §8.4), for a caller that needs one resolved field rather than
    /// a full conditions evaluation — see <see cref="AIReviewerPolicyVerdict"/>.
    ///
    /// <para>The same four fields <see cref="ApprovalConditionsRequest"/> carries, and nothing
    /// else: no reviews, no comments, no confidence score, because this answers a question about
    /// the POLICY, not about a round.</para>
    /// </summary>
    public class ResolveAIReviewerPolicyRequest
    {
        /// <inheritdoc cref="ApprovalConditionsRequest.CandidatePolicies"/>
        public required IReadOnlyList<ApprovalPolicy> CandidatePolicies { get; init; }

        /// <inheritdoc cref="ApprovalConditionsRequest.EntityType"/>
        public required string EntityType { get; init; }

        /// <inheritdoc cref="ApprovalConditionsRequest.ContentType"/>
        public required string? ContentType { get; init; }

        /// <inheritdoc cref="ApprovalConditionsRequest.IsPersonal"/>
        public required bool? IsPersonal { get; init; }
    }
}
