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
    /// Everything the "may this actor apply this decision?" question consults — HR-2, HR-3, HR-4's
    /// three routes and §8.6's regardless-rules, answered as one verdict.
    ///
    /// <para>It is one question rather than several because the rules are not independent: whether
    /// the conditions must be met depends on whether a bypass was requested, whether a bypass is
    /// available depends on the resolved policy, and whether the actor may decide at all depends
    /// on reviews that the condition count is reading anyway. Split into separate calls, a caller
    /// could satisfy each in turn and still assemble a sequence none of them permits.</para>
    ///
    /// <para>Every property is <c>required</c> for the reason given on
    /// <see cref="ApprovalConditionsRequest"/>.</para>
    /// </summary>
    public class DecideApprovalRequest
    {
        /// <summary>
        /// The user attempting to apply the decision.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// Which way they are moving it. Approving and rejecting are gated differently — see
        /// <see cref="ApprovalDecision"/>.
        /// </summary>
        public required ApprovalDecision Decision { get; init; }

        /// <summary>
        /// Every subject the actor could be authorised through. Holding a publisher-tier role for
        /// any one is enough.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }

        /// <summary>
        /// Every policy row that could apply, unresolved.
        /// </summary>
        public required IReadOnlyList<ApprovalPolicy> CandidatePolicies { get; init; }

        /// <summary>
        /// The entity type half of the policy key — the entity's own type.
        /// </summary>
        public required string EntityType { get; init; }

        /// <summary>
        /// The content type half of the policy key, or null for the entity-type default tier.
        /// </summary>
        public required string? ContentType { get; init; }

        /// <summary>
        /// The personality half of the policy key: whether the entity is a personal association
        /// (its <c>UserId</c> is set) — null for anything that is not an association, where the
        /// tier does not exist.
        /// </summary>
        public required bool? IsPersonal { get; init; }

        /// <summary>
        /// The <c>CreatedBy</c> of the content being decided on. HR-2 compares the actor against
        /// this.
        /// </summary>
        public required string EntityCreatedBy { get; init; }

        /// <summary>
        /// The approval's current state. A decision may only be applied while it is
        /// <c>Submitted</c> (§9.7.5).
        /// </summary>
        public required ApprovalState ApprovalState { get; init; }

        /// <summary>
        /// Whether the entity this approval is about has been soft-deleted (§9.7.6 rule 3).
        ///
        /// <para>Removal never touches the approval record, so a taken-down entity leaves its
        /// round standing at whatever status it held. This is the fact that stops the round
        /// being decided afterwards — and it is a property of the SUBJECT, not of the approval:
        /// <c>ApprovalState</c> would still read <c>Submitted</c>.</para>
        ///
        /// <para>An entity that could not be read at all reports <c>true</c> here for the same
        /// reason a deleted one does: neither is a subject a decision may be applied to, and
        /// failing closed is the only safe direction when the row behind an approval has
        /// gone.</para>
        /// </summary>
        public required bool IsSubjectDeleted { get; init; }

        /// <summary>
        /// Every review on the approval, including dismissed and soft-deleted ones — read both to
        /// count toward the threshold and to apply §8.6 regardless-rule 1, which bars anyone
        /// holding an <i>active</i> review from also deciding the round.
        /// </summary>
        public required IReadOnlyList<ReviewRecord> Reviews { get; init; }

        /// <summary>
        /// Every comment on the approval, including soft-deleted ones.
        /// </summary>
        public required IReadOnlyList<ApprovalCommentRecord> ApprovalComments { get; init; }

        /// <summary>
        /// The entity's confidence score, or null. Null never blocks; see
        /// <see cref="ApprovalConditionsRequest.ConfidenceScore"/>.
        /// </summary>
        public required decimal? ConfidenceScore { get; init; }

        /// <summary>
        /// Whether the actor is invoking HR-4 route 3 — approving <i>over</i> unmet conditions
        /// rather than because they are met.
        /// </summary>
        public required bool IsBypassRequested { get; init; }

        /// <summary>
        /// The reason recorded alongside a bypass.
        ///
        /// <para>Required in substance, not merely in form: bypass is only tolerable because it
        /// leaves a record, and a blank reason leaves one that says nothing. A bypass with no
        /// reason is refused.</para>
        /// </summary>
        public required string? BypassReason { get; init; }
    }
}
