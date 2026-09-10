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
using System.Linq;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    internal partial class AIReviewerOrchestrationService
    {
        // §7.9 rule 2 — the requesting tier is the whole review tier, everyone above the
        // read-only view. HR-3 does not narrow it: that rule bars a reviewer from SETTING an
        // approval status, and an assignment sets nothing.
        //
        // Matched by SUFFIX so the content-type-scoped roles of §18.6 rule 5 qualify, the same
        // way the verdict gate matches.
        //
        // THE SAME RULE the approval orchestration's ValidateUserMayRequestApprovalReviews asks,
        // duplicated rather than shared because the two services must not depend on one another
        // and the rule is five lines. It is ONE rule in two places: the admitted set, the suffix
        // matching and the refusal sentence are all identical on purpose — the sentence
        // deliberately still says "request approval reviews", because that is the tier being
        // named and a caller refused by either gate must not be able to tell them apart. If
        // either moves, move both.
        private static void ValidateUserMayRequestAIReviewer(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAIReviewerOrchestrationException(
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
                throw new UnauthorizedAIReviewerOrchestrationException(
                    message: "The current user is not allowed to request approval reviews.");
            }
        }

        private static void ValidateOnRetrieveAIReviewerStatus(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "AI reviewer orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        private static void ValidateOnRequestAIReviewer(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "AI reviewer orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        private static void ValidateOnWithdrawAIReviewer(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "AI reviewer orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        // Reported as not-found rather than as an empty status: a caller that cannot tell "no
        // approval exists" from "an approval exists and Berean has not been asked" would offer
        // the request control for a row with no round behind it.
        private static void ValidateStorageApprovalExists(
            ApprovalEntityMatch maybeMatch,
            EntityType entityType,
            Guid entityId)
        {
            if (maybeMatch is null)
            {
                throw new NotFoundAIReviewerOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
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
        // The same sentence the approval orchestration's pair throws, for the same reason and
        // across the same wire — the exposers return the INNER exception, so only the words
        // travel and a caller cannot tell which service refused them. THREE sites now say it. If
        // any one is ever reworded, reword all three.
        private static void ValidateStorageEntityIsVisible(
            bool isEntityVisible,
            EntityType entityType,
            Guid entityId)
        {
            if (isEntityVisible is false)
            {
                throw new NotFoundAIReviewerOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
            }
        }

        // §7.9 rule 7, asked over the resolved round rather than over a reviewer scope: only a
        // Submitted round accepts an assignment — before submission there is nothing to review,
        // and once it closes an assignment could never be answered (§7.7 rule 2b).
        //
        // The human sibling, ValidateApprovalRoundIsOpenForRequests, reads the identical field
        // off the scope it gathers. Same rule, same refusal sentence, and it must stay that way:
        // the two controls sit side by side in the same picker, so a moderator who is refused one
        // and not the other would be reading a difference that does not exist.
        private static void ValidateApprovalRoundIsOpenForAIReviewer(
            ApprovalEntityMatch approvalMatch,
            EntityType entityType,
            Guid entityId)
        {
            if (approvalMatch.ApprovalStatus != ApprovalStatus.Submitted)
            {
                throw new InvalidAIReviewerOrchestrationException(
                    message: $"Approval for {entityType} with id: {entityId} is not open for "
                        + "review requests. Its round is not submitted.");
            }
        }

        // Fail-closed (§8.4 rule 2): the resolved policy must say so explicitly, on every write,
        // never assumed from whatever the picker last showed the caller.
        //
        // The flag arrives from IAccessBroker.ResolveAIReviewerPolicyByIdAsync, which answers null
        // for a round it cannot resolve — and the caller collapses that null to false, so an
        // UNREAD policy reaches this gate looking exactly like a switched-off one. That is the
        // fail-closed reading and it is deliberate: a verdict nobody could read is not a
        // permission.
        private static void ValidateAIReviewerIsOffered(
            bool isAIReviewerOffered,
            EntityType entityType,
            Guid entityId)
        {
            if (isAIReviewerOffered is false)
            {
                throw new InvalidAIReviewerOrchestrationException(
                    message: $"The AI reviewer is not offered for {entityType} with id: "
                        + $"{entityId}.");
            }
        }

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
            var invalidAIReviewerOrchestrationException =
                new InvalidAIReviewerOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidAIReviewerOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidAIReviewerOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
