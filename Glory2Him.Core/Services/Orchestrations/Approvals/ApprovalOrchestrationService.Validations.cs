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
using Glory2Him.Core.Brokers.Integrities;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        // The bypass probe never becomes a bypass — nothing is written by a verdict read — but
        // the decision refuses a bypass with a blank reason (BypassReasonRequired) before it
        // reaches the question we are actually asking. So the probe carries a reason that says
        // what it is, and a real bypass supplies the human's own words.
        private const string BypassProbeReason =
            "Availability probe issued by the approval verdict. No bypass performed.";

        private static void ValidateOnRetrieveApprovalVerdict(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(Approval.EntityType)),
                (Rule: IsInvalid(entityId), Parameter: nameof(Approval.EntityId)));

        // Null-check first (a malformed fact names no row), then VERIFY THE SIGNATURE against
        // the event name this handler serves. The workflow is a receiver like any other, and
        // §14.6 rule 4 puts verification in the receiver rather than the transport precisely
        // because a handler is reachable without going through the broker.
        //
        // Without it, anyone able to put a message on a fact address could drive the approval
        // workflow: create approvals for rows they do not own, reset another round's reviews, or
        // trigger an evaluation that auto-approves. The signature binds the event name too, so a
        // genuine Tag-Added envelope cannot be replayed onto the Link handler.
        private ValueTask ValidateEntityFactEnvelopeAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            string eventName) =>
            ValidateEntityFactEnvelopeAsync(envelope, new[] { eventName });

        // The accepted set is the PUBLISHER'S COMPOSITION INVERTED:
        //
        //     { $"{entityName}{operation}" : operation maps to the address this handler binds }
        //
        // One name for every address but two. Removed and HardRemoved are published to ONE
        // address on purpose — consumers subscribe to a single removal address and tell the two
        // apart by the composed event name — so a handler bound there receives envelopes signed
        // under either, and a single expected name would refuse half of them. The event name is
        // inside the HMAC, so that refusal is silent: the fact is delivered, verified against a
        // name it was never signed with, and discarded.
        //
        // This is NOT a weakening to "accept any name". Both names in a removal set are names
        // THIS system signs FOR THIS ADDRESS. A TagAdded envelope verifies under neither, and
        // neither does a forgery.
        //
        // A null or empty set falls through the loop and throws, so "no name required" is
        // unreachable by construction rather than by a guard clause somebody can delete.
        private async ValueTask ValidateEntityFactEnvelopeAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            string[] acceptedEventNames)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");
            }

            foreach (string eventName in acceptedEventNames ?? Array.Empty<string>())
            {
                bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                    envelope, eventName, EnvelopeDirection.Request);

                if (isSignatureValid)
                {
                    return;
                }
            }

            throw new InvalidApprovalOrchestrationException(
                message: "Approval event is invalid. Integrity verification failed.");
        }

        private static void ValidateOnProcessApprovalInputsChanged(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateOnProcessEntity(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(Approval.EntityType)),
                (Rule: IsInvalid(entityId), Parameter: nameof(Approval.EntityId)));

        private static void ValidateOnDecideApproval(
            EntityType entityType,
            Guid entityId,
            ApprovalDecision decision,
            bool isBypassRequested,
            string bypassReason) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(Approval.EntityType)),
                (Rule: IsInvalid(entityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(decision), Parameter: nameof(ApprovalDecision)),

                // A bypass is only tolerable because it leaves a record, and an unexplained one
                // records nothing worth reading. Refused HERE — before any policy is read — so an
                // unexplained bypass fails under every policy, including one that would have
                // permitted the waiver (§9.7.5).
                (Rule: IsMissingBypassReason(isBypassRequested, bypassReason),
                    Parameter: nameof(bypassReason)));

        // The decision function answers with a reason; this turns its refusal into the layer's
        // own exception without repeating why. Re-deriving the reason here would put the policy
        // in a second place beside the one that owns it (§8.6.1 rule 4).
        private static void ValidateUserMayDecideApproval(AccessVerdict verdict)
        {
            if (verdict is null || verdict.IsPermitted is false)
            {
                throw new UnauthorizedApprovalOrchestrationException(
                    message: "The current user is not allowed to decide this approval.");
            }
        }

        private static dynamic IsInvalid(ApprovalDecision decision) => new
        {
            Condition = Enum.IsDefined(decision) is false,
            Message = "Value is not a recognized approval decision"
        };

        private static dynamic IsMissingBypassReason(
            bool isBypassRequested,
            string bypassReason) => new
            {
                Condition = isBypassRequested && string.IsNullOrWhiteSpace(bypassReason),
                Message = "Reason is required when a bypass is requested"
            };

        // The verdict names resolved policy — how many approvals are required, which block
        // fired — so it is the moderation view, not a public one (§16.7.2). §14.5 constrains
        // what an ERROR may reveal to an unprivileged probe; this is a deliberate answer to the
        // party the policy is addressed to, and the tier gate is what keeps those distinct.
        //
        // A reviewer is admitted: they cannot decide (HR-3) but the verdict is how they see
        // whether their own review completed the round, and they can already read the reviews
        // and comments individually.
        private static void ValidateUserMaySeeApprovalVerdict(SecurityContext securityContext)
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
                    message: "The current user is not allowed to view this approval verdict.");
            }
        }

        // Reported as not-found rather than as an empty verdict: a caller that cannot tell "no
        // approval exists" from "an approval exists and nothing blocks it" would show an enabled
        // approve button for a row with no approval behind it.
        private static void ValidateStorageApprovalExists(
            ApprovalEntityMatch maybeMatch,
            EntityType entityType,
            Guid entityId)
        {
            if (maybeMatch is null)
            {
                throw new NotFoundApprovalOrchestrationException(
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
        // If either sentence is ever reworded, reword both.
        private static void ValidateStorageEntityIsVisible(
            bool isEntityVisible,
            EntityType entityType,
            Guid entityId)
        {
            if (isEntityVisible is false)
            {
                throw new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
            }
        }

        // The broker returns null only when the approval vanished between the probe and the
        // evaluation. Treated as not-found rather than dereferenced, so a concurrent hard
        // removal surfaces as the same answer a missing approval gives.
        private static void ValidateStorageApprovalConditionsResolved(
            ApprovalConditionsVerdict conditions,
            EntityType entityType,
            Guid entityId)
        {
            if (conditions is null)
            {
                throw new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {entityType} with id: {entityId}.");
            }
        }

        // Where the codes become the sentences a moderator reads.
        //
        // Composed HERE rather than by the decision function: a policy engine that owns
        // user-facing English also owns presentation, and fixes one language into a package
        // every consumer shares. The client returns codes and the numbers behind them; this
        // turns them into something a screen can render, keeping the code alongside so a UI can
        // still branch without matching on prose (§16.7.2).
        private static IReadOnlyList<ApprovalBlockReason> ComposeBlockReasons(
            ApprovalStatus approvalStatus,
            ApprovalConditionsVerdict conditions,
            AccessVerdict decisionVerdict)
        {
            var reasons = new List<ApprovalBlockReason>();

            // Draft comes FIRST and alone. A draft has not entered a round, so the §8.5
            // conditions are not merely unmet but not yet asked — reporting "0 of 2 approvals"
            // beside it would invite a moderator to chase reviewers for something nobody has
            // submitted. The action it needs is to amend and submit (§16.7.3).
            if (approvalStatus == ApprovalStatus.Draft)
            {
                return new List<ApprovalBlockReason>
                {
                    new ApprovalBlockReason
                    {
                        Code = AccessDenialReason.BlockedDueToDraftStatus,
                        Message =
                            "This item has not been submitted for review yet. "
                                + "Submit it to start the approval process.",
                    },
                };
            }

            foreach (AccessDenialReason code in conditions.BlockReasons)
            {
                reasons.Add(new ApprovalBlockReason
                {
                    Code = code,
                    Message = DescribeBlockReason(code, conditions),
                });
            }

            // The caller-specific half. The conditions can be fully met and this caller still
            // barred — the author of the content (HR-2), or the reviewer whose own review
            // carried it over the line (§8.6 regardless-rule 1). Without it a verdict would
            // report nothing blocking while the approve button stayed disabled, which is the
            // one outcome guaranteed to look like a bug.
            bool isAlreadyReported = reasons.Any(reason =>
                reason.Code == decisionVerdict.DenialReason);

            if (decisionVerdict.IsPermitted is false
                && decisionVerdict.DenialReason != AccessDenialReason.None
                && isAlreadyReported is false)
            {
                reasons.Add(new ApprovalBlockReason
                {
                    Code = decisionVerdict.DenialReason,
                    Message = DescribeBlockReason(decisionVerdict.DenialReason, conditions),
                });
            }

            return reasons;
        }

        // Each message carries its own numbers where it has any — "1 of 3 required approvals"
        // rather than "approval threshold not met" — because a moderator needs to know how far
        // off it is, not merely that it is off.
        private static string DescribeBlockReason(
            AccessDenialReason code,
            ApprovalConditionsVerdict conditions) => code switch
            {
                AccessDenialReason.ApprovalThresholdNotMet =>
                    $"{conditions.ApprovalCount} of {conditions.RequiredNumberOfApprovals} "
                        + "required approvals recorded.",

                AccessDenialReason.BlockedByRejection =>
                    "A reviewer has rejected this item. The rejection must be withdrawn or "
                        + "the review round re-opened before it can be approved.",

                AccessDenialReason.BlockedByUnresolvedApprovalComment =>
                    conditions.UnresolvedApprovalCommentCount == 1
                        ? "1 review comment is still unresolved."
                        : $"{conditions.UnresolvedApprovalCommentCount} review comments are "
                            + "still unresolved.",

                AccessDenialReason.BlockedByZeroConfidenceScore =>
                    "This item scored zero on content confidence. Correct the score or "
                        + "approve with a bypass.",

                AccessDenialReason.BlockedDueToDraftStatus =>
                    "This item has not been submitted for review yet. "
                        + "Submit it to start the approval process.",

                AccessDenialReason.SelfApprovalNotPermitted =>
                    "You submitted this item, and self-approval is not permitted for this "
                        + "content type.",

                AccessDenialReason.ReviewerOnThisRoundMayNotDecide =>
                    "You have recorded a review on this round, so you may not also decide it.",

                AccessDenialReason.ReviewerMayNotDecide =>
                    "Reviewers record verdicts but do not decide approvals.",

                AccessDenialReason.NotInPublisherTier =>
                    "You do not hold the publisher role required to approve this item.",

                // Says the sanction applies, and does not say which scope of it fired. The
                // distinction is the sanction's own detail and names nothing the reader can act
                // on from here — no scope of it is appealable through the verdict (§18.6 rule 2).
                AccessDenialReason.BlockedByReadOnlyRole =>
                    "Your account is restricted to read-only for this content, so you cannot "
                        + "review or approve it.",

                AccessDenialReason.ApprovalNotOpenForReview =>
                    "This approval is not open for review.",

                AccessDenialReason.BypassNotPermitted =>
                    "Bypassing the approval rules is disabled for this content type.",

                AccessDenialReason.NotAuthenticated =>
                    "You are not signed in.",

                // Deliberately generic. A code with no sentence of its own is one this verdict
                // was not designed to explain, and inventing prose for it would state something
                // the evaluation never claimed.
                _ => "This item cannot be approved yet.",
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
            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
