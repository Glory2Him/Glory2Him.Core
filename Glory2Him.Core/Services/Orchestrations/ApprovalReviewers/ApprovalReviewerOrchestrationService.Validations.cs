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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    internal partial class ApprovalReviewerOrchestrationService
    {
        // 7.9 rule 2 - the requesting tier is the whole review tier, everyone above the
        // read-only view. HR-3 does not narrow it: that rule bars a reviewer from SETTING an
        // approval status, and an invitation sets nothing.
        //
        // Matched by SUFFIX so the content-type-scoped roles of 18.6 rule 5 qualify, the same
        // way the verdict gate matches.
        private static void ValidateUserMayRequestApprovalReviews(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalReviewerOrchestrationException(
                    message: "The current user is not authenticated.");
            }

            bool isPermitted =
                securityContext.Roles.Contains(Roles.Administrators)
                    || securityContext.Roles.Contains(Roles.Publishers)
                    || securityContext.Roles.Contains(Roles.Reviewers)
                    || securityContext.Roles.Any(role =>
                        role.EndsWith("-Publishers", StringComparison.Ordinal)
                            || role.EndsWith("-Reviewers", StringComparison.Ordinal));

            if (isPermitted is false)
            {
                throw new UnauthorizedApprovalReviewerOrchestrationException(
                    message: "The current user is not allowed to request approval reviews.");
            }
        }

        private static void ValidateOnRetrieveReviewerCandidates(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        // The same two parameters the candidates read takes, and now the resolver's ONLY shape
        // rule. The batch ceiling it used to carry is gone with the id list: the round decides
        // how many people there are to name, so there is no caller-supplied set left to bound.
        private static void ValidateOnRetrieveReviewerDisplayNames(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        private static void ValidateOnRequestApprovalReview(
            EntityType entityType,
            Guid entityId,
            string requestedUserId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"),

                (Rule: IsInvalid(requestedUserId),
                    Parameter: nameof(ApprovalReviewRequest.RequestedUserId)));

        private static void ValidateOnRetrieveApprovalReviewRequests(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        // Same three parameters the REQUEST takes, because withdrawal is now that operation's
        // exact undo — it names the round and the person rather than a row somebody had to have
        // been handed earlier.
        private static void ValidateOnWithdrawApprovalReviewRequest(
            EntityType entityType,
            Guid entityId,
            string requestedUserId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"),

                (Rule: IsInvalid(requestedUserId),
                    Parameter: nameof(ApprovalReviewRequest.RequestedUserId)));

        // The ONE not-found this service has. The entity-keyed gather answers null when no
        // approval carries the key, and it answers null again after a repair that could not run —
        // a taken-down entity, or one already decided (§9.8). Reported as not-found rather than
        // as a service fault: the caller's next move is the same either way.
        //
        // The sentence is CHARACTER-FOR-CHARACTER the one ValidateStorageEntityIsVisible throws,
        // and that is load-bearing rather than tidy: §14.5 rule 2 surfaces exception messages to
        // callers, so a missing round and a hidden entity a caller could tell apart would be one
        // refusal and one takedown oracle. If either is reworded, reword both.
        private static void ValidateStorageReviewerScopeResolved(
            ApprovalReviewerScope maybeScope,
            EntityType entityType,
            Guid entityId)
        {
            if (maybeScope is null)
            {
                throw new NotFoundApprovalReviewerOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
            }
        }

        // 7.9 rule 7. Only a Submitted round accepts invitations: before submission there is
        // nothing to review, and once it closes an invitation could never be answered - a review
        // may only be written while the approval is Submitted (7.7 rule 2b).
        private static void ValidateApprovalRoundIsOpenForRequests(
            ApprovalReviewerScope scope,
            EntityType entityType,
            Guid entityId)
        {
            if (scope.ApprovalStatus != ApprovalStatus.Submitted)
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: $"Approval for {entityType} with id: {entityId} is not open for "
                        + "review requests. Its round is not submitted.");
            }
        }

        // 7.9 rule 3, the owner half. HR-1 has no bypass and no setting relaxes it, so inviting
        // the owner would create an invitation the foundation would refuse to let them answer.
        private static void ValidateRequestedUserIsNotTheEntityOwner(
            ApprovalReviewerScope scope,
            string requestedUserId)
        {
            if (string.IsNullOrWhiteSpace(scope.EntityCreatedBy) is false
                && scope.EntityCreatedBy == requestedUserId)
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: "The requested user owns this entity and cannot be asked to "
                        + "review their own work.");
            }
        }


        // Rule 5 stops at the answer. This is the same test that used to refuse an INVITATION to
        // somebody who had already voted; it belongs here instead. Inviting them again is
        // harmless and now dissolves quietly (rule 4), but deleting the record of an invitation
        // they have answered rewrites how a standing verdict came about.
        private static void ValidateInvitationHasNotBeenAnswered(
            ApprovalReviewerScope scope,
            string requestedUserId)
        {
            if (scope.ActiveReviewerUserIds.Contains(requestedUserId))
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: "This review request has already been answered and can no longer " +
                        "be withdrawn.");
            }
        }

        // 7.9 rule 3, the tier half - resolved from the identity store rather than from the
        // caller. An invitation to somebody ineligible is a lie the panel would then render, and
        // one the foundation could not catch: a request row names no entity type, so nothing
        // downstream can tell a Tag-Reviewers holder from a Link-Reviewers one.
        private static void ValidateRequestedUserIsInTheReviewTier(
            IdentityUser requestedUser,
            string requestedUserId)
        {
            if (requestedUser is null)
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: $"User {requestedUserId} does not hold a review role for this "
                        + "entity, or is not an active account.");
            }
        }

        // Separate from the tier check above, and reported separately, because the two say
        // different things: one says the person was never eligible, this one says they were
        // and have since been restrained. Both refuse, and neither dissolves quietly — rule 4's
        // idempotence covers invitations that are redundant, not ones that can never be
        // answered.
        private static void ValidateRequestedUserIsNotBlocked(
            ISet<string> blockedUserIds,
            string requestedUserId)
        {
            if (blockedUserIds.Contains(requestedUserId))
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: $"User {requestedUserId} is restricted to read-only for this "
                        + "entity and cannot review it.");
            }
        }

        // §9.7.6 rule 3 / §14.5 rule 3. A removed subject is reported exactly as a missing one —
        // and "exactly" is load-bearing: the message is CHARACTER-FOR-CHARACTER the one
        // ValidateStorageApprovalExists throws, because §14.5 rule 2 says exception messages
        // surface outward to callers, so a message naming the ENTITY where the sibling names the
        // APPROVAL is itself the denial reason. Two refusals a caller can tell apart are one
        // refusal and one oracle: send a taken-down id and a random GUID, read the two bodies,
        // and learn which id used to exist.
        //
        // The same sentence the approval round's pair and the AI reviewer's pair throw, for the
        // same reason and across the same wire — the exposers return the INNER exception, so only
        // the words travel and a caller cannot tell which service refused them. If any one of
        // those sites is ever reworded, reword all of them.
        private static void ValidateStorageEntityIsVisible(
            bool isEntityVisible,
            EntityType entityType,
            Guid entityId)
        {
            if (isEntityVisible is false)
            {
                throw new NotFoundApprovalReviewerOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
            }
        }

        // Null-check first (a malformed fact names no row), then VERIFY THE SIGNATURE against the
        // event name this handler serves. A receiver verifies for itself rather than trusting the
        // transport (§14.6 rule 4), precisely because a handler is reachable without going through
        // the broker — and these two handlers write, so an unverified envelope reaching them would
        // let anyone able to put a message on the address clear a round's panel.
        //
        // The NAME is a literal, matching all fifteen existing receivers. EventBroker composes the
        // signed name as entityName + operation at publish time and exposes that composition to
        // nobody; #286 is the sweep that gives every site one derivation, and a bespoke one at
        // these two would make a third shape and pre-empt a ruling scoped to the rest. Safe here
        // for the reason that covers most of #286's sites: EventBroker.Approval.cs and
        // EventBroker.ApprovalReview.cs pass nameof(Approval) / nameof(ApprovalReview), so the
        // literal is the type name and cannot drift.
        //
        // REQUEST, not Reply, and getting it wrong is silent. The direction is bound into the HMAC
        // alongside the name, and EventBroker signs the publish leg as Request — so a receiver
        // asking for Reply would refuse every genuine envelope it was correctly delivered, with
        // nothing anywhere saying why.
        private async ValueTask ValidateEntityFactEnvelopeAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            string eventName)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: "Approval reviewer request is invalid, fix the errors and try again.");
            }

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalReviewerOrchestrationException(
                    message: "Approval reviewer event is invalid. Integrity verification failed.");
            }
        }

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(EntityType entityType) => new
        {
            Condition = Enum.IsDefined(entityType) is false,
            Message = "Value is not a recognized entity type"
        };

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidApprovalReviewerOrchestrationException =
                new InvalidApprovalReviewerOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalReviewerOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalReviewerOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
