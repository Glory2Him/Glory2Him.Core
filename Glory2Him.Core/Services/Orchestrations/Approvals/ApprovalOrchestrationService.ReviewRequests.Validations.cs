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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
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
                throw new UnauthorizedApprovalOrchestrationException(
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
                throw new UnauthorizedApprovalOrchestrationException(
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

        // The scope is gathered off an approval the caller-facing lookup just proved exists, so
        // a null here means the row vanished between the two reads. Reported as not-found rather
        // than as a service fault: the caller's next move is the same either way.
        private static void ValidateStorageReviewerScopeResolved(
            ApprovalReviewerScope maybeScope,
            EntityType entityType,
            Guid entityId)
        {
            if (maybeScope is null)
            {
                throw new NotFoundApprovalOrchestrationException(
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
                throw new InvalidApprovalOrchestrationException(
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
                throw new InvalidApprovalOrchestrationException(
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
                throw new InvalidApprovalOrchestrationException(
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
                throw new InvalidApprovalOrchestrationException(
                    message: $"User {requestedUserId} does not hold a review role for this "
                        + "entity, or is not an active account.");
            }
        }

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };
    }
}
