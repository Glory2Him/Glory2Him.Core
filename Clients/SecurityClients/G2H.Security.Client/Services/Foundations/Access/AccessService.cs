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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Services.Foundations.Access
{
    /// <summary>
    /// Approval policy, decided. Every method is a <b>pure function</b> of its request: no store,
    /// no clock, no ambient identity. That is what makes these rules testable as rules, and it is
    /// why the caller gathers — see the request types for why every section of a request is
    /// <c>required</c>.
    /// </summary>
    internal partial class AccessService : IAccessService
    {
        public ValueTask<ApprovalConditionsVerdict> EvaluateApprovalConditionsAsync(
            ApprovalConditionsRequest approvalConditionsRequest) =>
            TryCatch(() =>
            {
                ValidateOnEvaluateApprovalConditions(approvalConditionsRequest);

                ApprovalPolicy policy = ResolvePolicy(
                    approvalConditionsRequest.CandidatePolicies,
                    approvalConditionsRequest.EntityType,
                    approvalConditionsRequest.ContentType);

                return new ValueTask<ApprovalConditionsVerdict>(
                    EvaluateConditions(
                        policy,
                        approvalConditionsRequest.Reviews,
                        approvalConditionsRequest.ApprovalComments,
                        approvalConditionsRequest.ConfidenceScore));
            });

        public ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            RecordReviewRequest recordReviewRequest) =>
            TryCatch(() =>
            {
                ValidateOnRecordReview(recordReviewRequest);

                return new ValueTask<AccessVerdict>(
                    DecideMayRecordReview(recordReviewRequest));
            });

        public ValueTask<AccessVerdict> MayDecideApprovalAsync(
            DecideApprovalRequest decideApprovalRequest) =>
            TryCatch(() =>
            {
                ValidateOnDecideApproval(decideApprovalRequest);

                return new ValueTask<AccessVerdict>(
                    DecideMayDecideApproval(decideApprovalRequest));
            });

        // ── §7.7 rule 1 / HR-1 / §7.7 rule 2b / §8.9 rule 1 ─────────────────────────────────
        //
        public ValueTask<AccessVerdict> MayRecordApprovalCommentAsync(
            RecordApprovalCommentRequest recordApprovalCommentRequest) =>
            TryCatch(() =>
            {
                ValidateOnRecordApprovalComment(recordApprovalCommentRequest);

                return new ValueTask<AccessVerdict>(
                    DecideMayRecordApprovalComment(recordApprovalCommentRequest));
            });

        public ValueTask<AccessVerdict> MayAmendApprovalCommentAsync(
            AmendApprovalCommentRequest amendApprovalCommentRequest) =>
            TryCatch(() =>
            {
                ValidateOnAmendApprovalComment(amendApprovalCommentRequest);

                return new ValueTask<AccessVerdict>(
                    DecideMayAmendApprovalComment(amendApprovalCommentRequest));
            });

        public ValueTask<AccessVerdict> MayResolveApprovalCommentAsync(
            ResolveApprovalCommentRequest resolveApprovalCommentRequest) =>
            TryCatch(() =>
            {
                ValidateOnResolveApprovalComment(resolveApprovalCommentRequest);

                return new ValueTask<AccessVerdict>(
                    DecideMayResolveApprovalComment(resolveApprovalCommentRequest));
            });

        // Adding carries no tier: anyone who may contribute may speak on an approval they can
        // see, and that contribution gate is row-local and belongs to the foundation service
        // (§14.6). What is decided here is the pair of facts about the PARENT that a
        // single-entity service may not read for itself.
        private static AccessVerdict DecideMayRecordApprovalComment(RecordApprovalCommentRequest request)
        {
            if (IsActorUsable(request.Actor) is false)
            {
                return Refuse(
                    AccessDenialReason.NotAuthenticated,
                    "Actor is not authenticated or carries no resolvable user id.");
            }

            // Checked before the state so a taken-down approval reports what is actually wrong
            // with it rather than reporting a closed round.
            if (request.IsParentApprovalDeleted)
            {
                return Refuse(
                    AccessDenialReason.ParentApprovalUnavailable,
                    "The parent approval is soft-deleted and accepts no further comments.");
            }

            if (request.ApprovalState != ApprovalState.Submitted)
            {
                return Refuse(
                    AccessDenialReason.ApprovalNotOpenForComment,
                    "The parent approval is not open for comment — it is either not yet submitted, or its round has closed.");
            }

            return Permit("Actor may add a comment to an open approval.");
        }

        // Editing the text and withdrawing the row ask the same question, so they share one
        // decision. No role widens it: a comment belongs to whoever wrote it.
        private static AccessVerdict DecideMayAmendApprovalComment(AmendApprovalCommentRequest request)
        {
            if (IsActorUsable(request.Actor) is false)
            {
                return Refuse(
                    AccessDenialReason.NotAuthenticated,
                    "Actor is not authenticated or carries no resolvable user id.");
            }

            if (IsSameUser(request.Actor.UserId, request.CommentCreatedBy) is false)
            {
                return Refuse(
                    AccessDenialReason.NotApprovalCommentAuthor,
                    "Actor did not write this comment; no role permits amending another's words.");
            }

            if (request.IsParentApprovalDeleted)
            {
                return Refuse(
                    AccessDenialReason.ParentApprovalUnavailable,
                    "The parent approval is soft-deleted, so its comments are no longer writable.");
            }

            if (request.ApprovalState != ApprovalState.Submitted)
            {
                return Refuse(
                    AccessDenialReason.ApprovalNotOpenForComment,
                    "The parent approval is not open — before submission there is no thread, and once it closes what was said stands as recorded.");
            }

            return Permit("Actor is the author of the comment and the round is open.");
        }

        // The one comment operation an Admin may perform on someone else's row, and deliberately
        // the only one: resolving records that a comment is settled — that it no longer requires
        // anything before the approval can proceed — which changes no words.
        private static AccessVerdict DecideMayResolveApprovalComment(
            ResolveApprovalCommentRequest request)
        {
            if (IsActorUsable(request.Actor) is false)
            {
                return Refuse(
                    AccessDenialReason.NotAuthenticated,
                    "Actor is not authenticated or carries no resolvable user id.");
            }

            bool isAuthor = IsSameUser(request.Actor.UserId, request.CommentCreatedBy);
            bool isAdmin = request.Actor.Roles.Contains(RoleNames.Admin);

            if (isAuthor is false && isAdmin is false)
            {
                return Refuse(
                    AccessDenialReason.NotApprovalCommentAuthor,
                    "Actor is neither the comment's author nor an Admin resolving on their behalf.");
            }

            if (request.IsParentApprovalDeleted)
            {
                return Refuse(
                    AccessDenialReason.ParentApprovalUnavailable,
                    "The parent approval is soft-deleted, so its comments are no longer writable.");
            }

            if (request.ApprovalState != ApprovalState.Submitted)
            {
                return Refuse(
                    AccessDenialReason.ApprovalNotOpenForComment,
                    "The parent approval is not open — before submission nothing reads the flag, and once it closes the flags are final.");
            }

            return isAuthor
                ? Permit("Actor is the author of the comment and the round is open.")
                : Permit("Actor is an Admin resolving on the author's behalf; UpdatedBy records them.");
        }

        // Order matters and is not arbitrary. Identity comes first, then role, then the rules
        // that need no policy at all — because a caller who is refused outright must not learn
        // anything about the approval's state or its configuration on the way out (§14.5).
        private static AccessVerdict DecideMayRecordReview(RecordReviewRequest request)
        {
            if (IsActorUsable(request.Actor) is false)
            {
                return Refuse(
                    AccessDenialReason.NotAuthenticated,
                    "Actor is not authenticated or carries no resolvable user id.");
            }

            if (HasReviewTier(request.Actor, request.RoleSubjects) is false)
            {
                return Refuse(
                    AccessDenialReason.NotInReviewTier,
                    "Actor holds no review-tier role for any subject of this entity.");
            }

            // HR-1 is unconditional. There is no setting to consult here and deliberately no
            // policy on the request: a review is one person vouching for another's work, and a
            // threshold met by self-reviews is not a threshold.
            if (IsSameUser(request.Actor.UserId, request.EntityCreatedBy))
            {
                return Refuse(
                    AccessDenialReason.SelfReviewNeverPermitted,
                    "Actor is the author of the content under review (HR-1).");
            }

            if (request.ApprovalState != ApprovalState.Submitted)
            {
                return Refuse(
                    AccessDenialReason.ApprovalNotOpenForReview,
                    $"The approval is {request.ApprovalState}, not Submitted, "
                        + "so the review round is not open (§7.7 rule 2b).");
            }

            // §7.7 rule 1 bars a SECOND active review, not every write — a reviewer amending
            // their own standing verdict after a conversation is the normal path, and is what
            // the amendment flag distinguishes.
            if (request.IsAmendingOwnReview is false
                && HasActiveReviewBy(request.ExistingReviews, request.Actor.UserId))
            {
                return Refuse(
                    AccessDenialReason.ActiveReviewAlreadyRecorded,
                    "Actor already holds an active review on this approval (§7.7 rule 1).");
            }

            return Permit("Actor may record a review on this approval.");
        }

        // ── HR-2 / HR-3 / HR-4 / §8.6 regardless-rule 1 ─────────────────────────────────────
        private static AccessVerdict DecideMayDecideApproval(DecideApprovalRequest request)
        {
            if (IsActorUsable(request.Actor) is false)
            {
                return Refuse(
                    AccessDenialReason.NotAuthenticated,
                    "Actor is not authenticated or carries no resolvable user id.");
            }

            // HR-3. The publisher tier is checked rather than "not a reviewer", so a user with
            // neither tier is refused too. The distinct reason is reported only when the actor
            // does hold a review role — that is the case worth naming in a log, because it is
            // someone being told their role is a different job rather than a weaker one.
            if (HasPublisherTier(request.Actor, request.RoleSubjects) is false)
            {
                return HasReviewTier(request.Actor, request.RoleSubjects)
                    ? Refuse(
                        AccessDenialReason.ReviewerMayNotDecide,
                        "Actor holds a review-tier role but not the publisher tier (HR-3).")
                    : Refuse(
                        AccessDenialReason.NotInPublisherTier,
                        "Actor holds no publisher-tier role for any subject of this entity.");
            }

            if (request.ApprovalState != ApprovalState.Submitted)
            {
                return Refuse(
                    AccessDenialReason.ApprovalNotOpenForReview,
                    $"The approval is {request.ApprovalState}, not Submitted, "
                        + "so there is no open round to decide.");
            }

            // §8.6 regardless-rule 1. Checked BEFORE the self-approval setting, because no
            // setting relaxes it: a publisher who filed a review has spent their vote on this
            // round whatever AllowSelfApproval says.
            if (HasActiveReviewBy(request.Reviews, request.Actor.UserId))
            {
                return Refuse(
                    AccessDenialReason.ReviewerOnThisRoundMayNotDecide,
                    "Actor holds an active review on this entity, so another decider must "
                        + "apply the outcome (§8.6 regardless-rule 1).");
            }

            ApprovalPolicy policy = ResolvePolicy(
                request.CandidatePolicies,
                request.EntityType,
                request.ContentType);

            // A rejection withholds approval rather than granting it. Nothing is being waived,
            // so neither the threshold nor the bypass lock applies (§9.7.5) — and HR-2 does not
            // either: refusing your own work is not self-approval.
            if (request.Decision == ApprovalDecision.Reject)
            {
                return Permit("Actor may reject this approval.");
            }

            if (IsSameUser(request.Actor.UserId, request.EntityCreatedBy)
                && policy.AllowSelfApproval is false)
            {
                return Refuse(
                    AccessDenialReason.SelfApprovalNotPermitted,
                    "Actor is the author and the resolved policy does not allow "
                        + "self-approval (HR-2).");
            }

            if (request.IsBypassRequested)
            {
                if (policy.DoNotAllowBypassingSettings)
                {
                    return Refuse(
                        AccessDenialReason.BypassNotPermitted,
                        "The resolved policy closes the bypass route entirely (HR-4 route 3).");
                }

                if (string.IsNullOrWhiteSpace(request.BypassReason))
                {
                    return Refuse(
                        AccessDenialReason.BypassReasonRequired,
                        "A bypass must record why it was used.");
                }

                // The conditions are evaluated even though the answer cannot change the outcome,
                // because WHAT was waived is the whole value of the record. A bypass over a
                // standing rejection and a bypass over nothing are different events, and without
                // this they would leave identical audit trails — the first being precisely the
                // one anybody would later go looking for.
                ApprovalConditionsVerdict bypassedConditions = EvaluateConditions(
                    policy,
                    request.Reviews,
                    request.ApprovalComments,
                    request.ConfidenceScore);

                return PermitByBypass(
                    bypassedConditions.BlockReason,
                    bypassedConditions.AreConditionsMet
                        ? "Actor may approve this entity by bypass (HR-4 route 3), though the "
                            + "conditions were already met — nothing was waived."
                        : "Actor may approve this entity by bypass (HR-4 route 3), waiving: "
                            + bypassedConditions.Explanation);
            }

            ApprovalConditionsVerdict conditions = EvaluateConditions(
                policy,
                request.Reviews,
                request.ApprovalComments,
                request.ConfidenceScore);

            if (conditions.AreConditionsMet is false)
            {
                return Refuse(
                    conditions.BlockReason,
                    "The approval conditions are not met and no bypass was requested. "
                        + conditions.Explanation);
            }

            return Permit("Actor may approve this entity (HR-4 route 1).");
        }

        // ── §8.5, evaluated in the order the formula states ──────────────────────────────────
        private static ApprovalConditionsVerdict EvaluateConditions(
            ApprovalPolicy policy,
            IReadOnlyList<ReviewRecord> reviews,
            IReadOnlyList<ApprovalCommentRecord> comments,
            decimal? confidenceScore)
        {
            // The Dismissed clause is redundant TODAY and kept on purpose. A dismissed review is
            // neither Approved nor Rejected, so neither the count below nor the rejection check
            // can see it either way — no test can distinguish its presence, and mutation testing
            // correctly reports it as an equivalent mutant. It stays because `activeReviews` is a
            // named concept that other rules will read, and the first rule to ask "is there any
            // active review?" rather than "is there an approving one?" would silently get the
            // wrong answer without it.
            IReadOnlyList<ReviewRecord> activeReviews = reviews
                .Where(review => review.IsDeleted is false
                    && review.Verdict != ReviewVerdict.Dismissed)
                .ToList();

            int approvalCount = activeReviews
                .Count(review => review.Verdict == ReviewVerdict.Approved);

            int required = policy.RequireApprovals
                ? policy.RequiredNumberOfApprovals
                : 0;

            AccessDenialReason blockReason = AccessDenialReason.None;
            string explanation;

            if (policy.RequireApprovals && approvalCount < required)
            {
                blockReason = AccessDenialReason.ApprovalThresholdNotMet;
                explanation = $"{approvalCount} of {required} required approvals recorded.";
            }
            else if (policy.RequireApprovals
                && policy.BlockOnReject
                && activeReviews.Any(review => review.Verdict == ReviewVerdict.Rejected))
            {
                blockReason = AccessDenialReason.BlockedByRejection;
                explanation = "An active rejection blocks this approval.";
            }
            else if (policy.RequireReviewCommentResolutionBeforeApprovals
                && comments.Any(comment => comment.IsDeleted is false
                    && comment.IsResolved is false))
            {
                blockReason = AccessDenialReason.BlockedByUnresolvedApprovalComment;
                explanation = "An approval comment is still unresolved.";
            }
            else if (policy.BlockOnZeroApprovalScore && confidenceScore == 0m)
            {
                // Only an explicit zero blocks. Null means the confidence process has not run,
                // not that the entity was judged worthless (§8.5 rule 8).
                blockReason = AccessDenialReason.BlockedByZeroConfidenceScore;
                explanation = "The entity's confidence score is zero.";
            }
            else
            {
                explanation = policy.RequireApprovals
                    ? $"Conditions met with {approvalCount} of {required} required approvals."
                    : "Conditions trivially met: the policy does not require approvals.";
            }

            bool conditionsMet = blockReason == AccessDenialReason.None;

            return new ApprovalConditionsVerdict
            {
                AreConditionsMet = conditionsMet,
                ShouldAutoApprove =
                    conditionsMet && policy.AutoApproveIfAllApprovalRequirementsMet,
                BlockReason = blockReason,
                ApprovalCount = approvalCount,
                RequiredNumberOfApprovals = required,
                Explanation = explanation,
            };
        }

        // ── §8.4: most specific wins, WHOLESALE ──────────────────────────────────────────────
        //
        // The first matching row supplies EVERY field; fields are never merged across tiers.
        // Specificity is a property of the MATCH, not of the values — a narrow row may
        // legitimately loosen policy, and a merge would let a narrow row that meant to loosen
        // one field silently inherit six others it never mentioned.
        private static ApprovalPolicy ResolvePolicy(
            IReadOnlyList<ApprovalPolicy> candidatePolicies,
            string entityType,
            string? contentType)
        {
            ApprovalPolicy? contentTypeTier = contentType is null
                ? null
                : candidatePolicies.FirstOrDefault(policy =>
                    Matches(policy.EntityType, entityType)
                        && policy.ContentType is not null
                        && Matches(policy.ContentType, contentType));

            if (contentTypeTier is not null)
            {
                return contentTypeTier;
            }

            ApprovalPolicy? entityTypeTier = candidatePolicies.FirstOrDefault(policy =>
                Matches(policy.EntityType, entityType) && policy.ContentType is null);

            return entityTypeTier ?? ApprovalPolicyDefaults.SystemDefaultFor(
                entityType,
                contentType);
        }

        // ── Role tiers (§8.9, §18.6) ─────────────────────────────────────────────────────────
        //
        // One subject is enough. For an association that means a publisher trusted with either
        // endpoint may act, which is the same one-endpoint-is-enough reasoning the entity's own
        // read gates use: the pairing is the thing being decided, and someone trusted with one
        // end can see both.
        private static bool HasReviewTier(
            AccessActor actor,
            IReadOnlyList<RoleSubject> roleSubjects) =>
            actor.Roles.Contains(RoleNames.Reviewer)
                || HasPublisherTier(actor, roleSubjects)
                || roleSubjects.Any(subject => HasScopedRole(
                    actor,
                    subject,
                    RoleNames.ReviewerFor,
                    RoleNames.ReviewerFor));

        private static bool HasPublisherTier(
            AccessActor actor,
            IReadOnlyList<RoleSubject> roleSubjects) =>
            actor.Roles.Contains(RoleNames.Publisher)
                || actor.Roles.Contains(RoleNames.Admin)
                || roleSubjects.Any(subject => HasScopedRole(
                    actor,
                    subject,
                    RoleNames.PublisherFor,
                    RoleNames.PublisherFor));

        // The narrow tier widens into the coarse one: ContentItem-Blog-Reviewer ⊂
        // ContentItem-Reviewer ⊂ Reviewer (§18.6 rule 4). Holding either spelling satisfies the
        // check for that content type; the narrow one never satisfies a check for a different one.
        private static bool HasScopedRole(
            AccessActor actor,
            RoleSubject subject,
            Func<string, string> composeForEntityType,
            Func<string, string, string> composeForContentType)
        {
            if (actor.Roles.Contains(composeForEntityType(subject.EntityType)))
            {
                return true;
            }

            return subject.ContentType is not null
                && actor.Roles.Contains(
                    composeForContentType(subject.EntityType, subject.ContentType));
        }

        private static bool HasActiveReviewBy(
            IReadOnlyList<ReviewRecord> reviews,
            string userId) =>
            reviews.Any(review =>
                review.IsDeleted is false
                    && review.Verdict != ReviewVerdict.Dismissed
                    && IsSameUser(review.CreatedBy, userId));

        private static bool IsActorUsable(AccessActor actor) =>
            actor.IsAuthenticated && string.IsNullOrWhiteSpace(actor.UserId) is false;

        // Blank never matches blank. Both a missing actor id and a missing author would
        // otherwise compare equal, turning "is this the author?" into "yes" for every row whose
        // author was never stamped.
        private static bool IsSameUser(string? first, string? second) =>
            string.IsNullOrWhiteSpace(first) is false
                && string.IsNullOrWhiteSpace(second) is false
                && string.Equals(first, second, StringComparison.Ordinal);

        private static bool Matches(string first, string second) =>
            string.Equals(first, second, StringComparison.Ordinal);

        private static AccessVerdict Permit(string explanation) =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = explanation,
            };

        // Permitted along the bypass route. DenialReason stays None — this is a permission, and a
        // caller checking `reason != None` must not see a refusal here.
        //
        // IsBypassUsed reports whether anything was ACTUALLY waived, not whether the bypass route
        // was taken. Requesting a bypass over an approval whose conditions were already met waives
        // nothing, and the caller writes this flag into a column whose entire purpose is answering
        // "what was published without meeting its conditions" — so reporting true there would
        // enter a false positive into the one query the record exists to serve. That reading is
        // also what IsBypassUsed documents about itself.
        private static AccessVerdict PermitByBypass(
            AccessDenialReason bypassedBlockReason,
            string explanation) =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = bypassedBlockReason != AccessDenialReason.None,
                BypassedBlockReason = bypassedBlockReason,
                Explanation = explanation,
            };

        private static AccessVerdict Refuse(AccessDenialReason reason, string explanation) =>
            new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = reason,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = explanation,
            };
    }
}
