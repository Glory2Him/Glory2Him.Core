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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    internal partial class ReactionService
    {
        private static void ValidateOnSubmitReaction(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        private static void ValidateOnApproveReaction(Reaction reaction) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reaction.Id), Parameter: nameof(Reaction.Id)),

                // Approve owns the whole of IApproval, so it is the one operation allowed to
                // carry these — but only to an outcome the approval workflow can produce. Draft
                // and Submitted are states the row LEAVES here, not ones approving may set, and
                // Dismissed belongs to a later withdrawal step.
                (Rule: IsNotAnApprovalOutcome(reaction.ApprovalStatus),
                    Parameter: nameof(Reaction.ApprovalStatus)),

                // publication is a consequence of approval — a row cannot be published while
                // being rejected, and a publish date without publication is a date nothing
                // reads
                (Rule: IsPublishedWithoutApproval(
                        reaction.ApprovalStatus, reaction.IsPublished),
                    Parameter: nameof(Reaction.IsPublished)),

                (Rule: IsPublishDateWithoutPublication(
                        reaction.IsPublished, reaction.PublishDate),
                    Parameter: nameof(Reaction.PublishDate)));

        // Submitting is the owner-or-publisher act of §9.2. It is deliberately the SAME set the
        // modify carve-out admits (design §9.2 rules 4-6): a dedicated status-only verb must
        // not be narrower than the identical transition reached through a content edit. A
        // Reviewer is absent by design — HasPublisherRole excludes the review tier (§8.6 HR-3),
        // and a Reviewer moves an outcome only through the approval workflow, never by hand.
        private async ValueTask ValidateUserCanSubmitStorageReactionAsync(
            Reaction storageReaction,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageReaction.CreatedBy == actorUserId;

            bool isPermitted =
                isOwner
                    || HasPublisherRole(securityContext);

            if (isPermitted is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to submit this reaction.");
            }
        }

        // Approving is the PUBLISHER-tier decision, and it is the narrowest gate in the service
        // because this is the only path by which a reaction becomes publicly visible.
        //
        // Two hard rules meet here (design §8.6):
        //
        // HR-3 — a Reviewer may NEVER set an approval status. A reviewer's instrument is the
        // ApprovalReview record; they move the outcome only indirectly, through automatic
        // approval. HasPublisherRole is strictly narrower than the review tier and excludes the
        // Reviewer roles for exactly this reason.
        //
        // HR-2 — no one approves their own content unless AllowSelfApproval permits it. That
        // setting lives on another entity, so the question goes to IAccessBroker.
        //
        // Together they are what stop a contributor walking the whole path alone: create,
        // submit, approve, publish.
        //
        // The row-local publisher check below is kept even though the access decision repeats
        // it. It is not redundancy for its own sake: it is what makes an unauthorised caller
        // cost one role comparison instead of several table reads, and it means a defect in the
        // gathering can only ever make this gate stricter, never open it.
        //
        // Returns the verdict rather than only throwing on refusal, because the caller has to
        // write IsApprovedByBypass from it. Those two IApproval members are the one part of the
        // interface the approve operation DERIVES instead of accepting: they exist to record
        // that the conditions were waived, and a caller able to set them is equally able to
        // clear them, erasing the one event they are here to capture (design §9.7.1 rule 3).
        private async ValueTask<AccessVerdict> ValidateUserCanApproveStorageReactionAsync(
            Reaction storageReaction,
            Reaction reaction,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            if (HasPublisherRole(securityContext) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to approve this reaction.");
            }

            AccessVerdict verdict = await this.accessBroker.MayDecideApprovalAsync(
                new ApprovalDecisionQuery
                {
                    EntityType = EntityType.Reaction,
                    EntityId = storageReaction.Id,

                    // A reaction carries no content type, so its policy tier is (Reaction, null) — the
                    // same shape an association uses. There is exactly one tier to resolve
                    // against.
                    ContentType = null,

                    // One subject: the reaction authorises from itself, keyed by its own type with
                    // no content type.
                    RoleSubjects = new List<RoleSubject>
                    {
                        new RoleSubject
                        {
                            EntityType = EntityType.Reaction.ToString(),
                            ContentType = null,
                        },
                    },

                    // From STORAGE. Taking the author from the caller's copy would let a
                    // contributor name someone else as author and approve their own row.
                    EntityCreatedBy = storageReaction.CreatedBy,

                    // A reaction has no confidence score — that is an association's input. The
                    // decision engine treats a null score as "no score to weigh".
                    ConfidenceScore = null,

                    Decision = reaction.ApprovalStatus == ApprovalStatus.Rejected
                        ? ApprovalDecision.Reject
                        : ApprovalDecision.Approve,

                    // Bypass is its own operation and this is not it; an ordinary approve never
                    // claims one. Passing the caller's flag here would make every approve a
                    // potential bypass.
                    IsBypassRequested = false,
                    BypassReason = null,

                    SecurityContext = securityContext,
                },
                cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason is logged server-side and the caller is told nothing
                // about the policy. The verdict's explanation names resolved settings — how
                // many approvals were required, which block fired — and exception messages and
                // their Data surface outward through a public event address.
                await this.loggingBroker.LogWarningAsync(
                    $"Reaction approval denied for {storageReaction.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to approve this reaction.");
            }

            return verdict;
        }

        // Only a row actually in review can be decided. Approving a Draft would skip the
        // submission the workflow is built around, and approving an already-decided row would
        // re-publish a verdict.
        private static void ValidateStorageReactionIsApprovable(
            Reaction storageReaction)
        {
            if (storageReaction.ApprovalStatus != ApprovalStatus.Submitted)
            {
                throw new InvalidReactionException(
                    message: "Reaction cannot be approved from status " +
                        $"{storageReaction.ApprovalStatus}.");
            }
        }

        // Only a Draft may be submitted. A row already Submitted, Approved, Rejected or
        // Dismissed is not a fresh submission, and re-submitting one would either re-open a
        // decided item or re-announce a pending one (design §9.7.1, issue #111 case 7).
        private static void ValidateStorageReactionIsSubmittable(
            Reaction storageReaction)
        {
            if (storageReaction.ApprovalStatus != ApprovalStatus.Draft)
            {
                throw new InvalidReactionException(
                    message: "Reaction cannot be submitted from status " +
                        $"{storageReaction.ApprovalStatus}.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transitions become a probe for which reactions used to exist.
        private static void ValidateStorageReactionIsNotDeleted(
            Reaction storageReaction,
            Guid reactionId)
        {
            if (storageReaction.IsDeleted)
            {
                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }
        }

        private static dynamic IsNotAnApprovalOutcome(ApprovalStatus approvalStatus) => new
        {
            Condition =
                approvalStatus != ApprovalStatus.Approved
                    && approvalStatus != ApprovalStatus.Rejected,

            Message = "Approval status must be Approved or Rejected."
        };

        private static dynamic IsPublishedWithoutApproval(
            ApprovalStatus approvalStatus,
            bool isPublished) => new
            {
                Condition = isPublished && approvalStatus != ApprovalStatus.Approved,
                Message = "Is published requires an approved reaction."
            };

        private static dynamic IsPublishDateWithoutPublication(
            bool isPublished,
            DateTimeOffset? publishDate) => new
            {
                Condition = isPublished is false && publishDate.HasValue,
                Message = "Publish date requires a published reaction."
            };
    }
}
