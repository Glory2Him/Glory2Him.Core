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
    /// The system default policy, used when no candidate row matches at all (design §8.4 rule 2).
    ///
    /// <para><b>It is spelled out here rather than taken from a default-constructed
    /// <see cref="ApprovalPolicy"/>, and that is not a stylistic choice.</b> The consuming
    /// application's stored-entity property initialisers are the defaults for a row somebody is
    /// creating; they are not the policy for an environment that has no row. The two disagree on
    /// <c>BlockOnReject</c> — the entity initialises it to <c>false</c>, while §8.4 rule 2
    /// requires <c>true</c> when nothing resolves. Falling back to a constructed instance would
    /// therefore have been silently more permissive than the design on exactly one field, in
    /// exactly the situation the fail-closed rule exists for: an unseeded environment.</para>
    ///
    /// <para>Every value below is the strict reading. A missing configuration row must never mean
    /// "no approval needed" — an unseeded environment would otherwise publish everything.</para>
    /// </summary>
    public static class ApprovalPolicyDefaults
    {
        /// <summary>
        /// Builds the fail-closed system default for a given policy key.
        /// </summary>
        public static ApprovalPolicy SystemDefaultFor(string entityType, string? contentType) =>
            new ApprovalPolicy
            {
                EntityType = entityType,
                ContentType = contentType,
                IsPersonal = null,

                // §8.4 rule 2, verbatim.
                RequireApprovals = true,
                RequiredNumberOfApprovals = 1,
                AutoApproveIfAllApprovalRequirementsMet = false,
                AllowSelfApproval = false,
                BlockOnReject = true,
                RequireReapprovalOnChange = true,
                DoNotAllowBypassingSettings = false,

                // §8.4 rule 2 does not name these two, though §8.5 and HR-4 route 1 both depend
                // on them, so their system default was simply unstated. Both take the strict
                // reading the rest of the rule takes. Blocking on a zero score cannot deadlock
                // anything: §8.5 rule 8 is explicit that a NULL score does not block, and an
                // unscored entity is null rather than zero.
                BlockOnZeroApprovalScore = true,
                RequireReviewCommentResolutionBeforeApprovals = true,

                // §8.6.2 is not named by §8.4 rule 2 either — Berean postdates it — but the same
                // fail-closed reading applies: an unseeded environment offers no AI reviewer and
                // casts no AI vote. Both false satisfies CK_ApprovalSetting_AIVoteRequiresAIReviewer
                // on its own terms, so there is no ordering to get wrong here.
                IsAIReviewerOffered = false,
                IsAIAllowedToVote = false,

                // Never read while IsAIAllowedToVote is false (§8.6.2), which it always is here —
                // 0.00 is not a claim about where a real deployment should set these, only that
                // the system default casts no vote to threshold in the first place.
                AIApprovalConfidenceRejectionThreshold = 0.00m,
                AIApprovalConfidenceApprovalThreshold = 0.00m,
            };
    }
}
