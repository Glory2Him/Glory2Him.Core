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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    internal partial class ReactionService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ReactionReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is blocked from contributing reactions.");
            }
        }

        // the moderation roles that may act on and read non-public versions for review and
        // audit (Reviewers, Publishers, Administrators — global or Reaction-scoped, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewers)
                || securityContext.Roles.Contains(Roles.ReactionReviewers)
                || securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.ReactionPublishers)
                || securityContext.Roles.Contains(Roles.Administrators);

        // the publisher tier: the roles the approve operation itself requires, and the only ones
        // besides the owner that may move a submission status through the general modify. Strictly
        // narrower than the review tier — a reviewer is absent by design (§8.6 HR-3).
        private static bool HasPublisherRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.ReactionPublishers)
                || securityContext.Roles.Contains(Roles.Administrators);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower process rules stay in the orchestration.
        //
        // Returns whether the caller may also use the Draft <-> Submitted carve-out (§9.2): the
        // owner or the Publishers tier. It falls out of the ownership check already performed, so
        // it is returned rather than recomputed. A reviewer holds write permission but is NOT in
        // the publisher tier, so it may amend content and still never move the status (HR-3).
        private async ValueTask<bool> ValidateUserCanModifyStorageReactionAsync(
            Reaction storageReaction,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageReaction.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to modify this reaction.");
            }

            return isOwner || HasPublisherRole(securityContext);
        }

        // Approved and Rejected are TERMINAL: the content of a row in either state is immutable
        // in place, to its owner, to a publisher and to an administrator alike (§3.4 rules 7 and 16,
        // §9.7.4, §12.3.1 shared rule 9). Reviewers reached a verdict on that text, and text
        // that changes underneath a verdict makes the verdict a record of nothing.
        //
        // This is NOT the rule the status pin enforces, and the two are easy to confuse. The pin
        // refuses a CHANGE to ApprovalStatus, and its condition is guarded by
        // inputStatus != storageStatus — so a caller who amends an approved row while echoing
        // the stored status back unchanged passes it, and the content is written through with
        // IsPublished and PublishDate still at their approved values. The edit then goes public
        // with no re-review. That is the hole this closes; the pin never covered it.
        //
        // A reaction never forks, so refusing the write IS the enforcement — there is no version to
        // amend into, and the only way back is the Administrators override on the approval transition
        // (§8.6 HR-4). For the Versioned entities an orchestration turns this same condition
        // into a fork instead (§10.17, #199).
        //
        // The §9.2 Draft <-> Submitted carve-out is unreachable from here and stays that way:
        // it is only ever reached from Draft or Submitted, so a terminal row is refused before
        // the carve-out is consulted.
        private static void ValidateStorageReactionIsNotTerminal(Reaction storageReaction)
        {
            bool isTerminal =
                storageReaction.ApprovalStatus == ApprovalStatus.Approved
                    || storageReaction.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTerminal)
            {
                throw new InvalidReactionException(
                    message: "Reaction cannot be modified from status " +
                        $"{storageReaction.ApprovalStatus}.");
            }
        }

        // removing content is a takedown, not a moderation step — the owner may remove
        // their own reaction and an administrator may remove anyone's; Reviewers and Publishers
        // moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageReactionAsync(
            Reaction storageReaction,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageReaction.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to remove this reaction.");
            }
        }

        // a hard remove destroys the row and its audit trail — Administrators only
        private static void ValidateUserCanHardRemoveReaction(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly)
                || securityContext.Roles.Contains(Roles.ReactionReadOnly))
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is blocked from contributing reactions.");
            }

            if (securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedReactionException(
                    message: "The current user is not allowed to permanently remove this reaction.");
            }
        }

        private async ValueTask ValidateOnAddReactionAsync(
            Reaction reaction,
            SecurityContext securityContext)
        {
            ValidateReactionIsNotNull(reaction);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reaction.Id), Parameter: nameof(Reaction.Id)),
                (Rule: IsInvalid(reaction.Name), Parameter: nameof(Reaction.Name)),
                (Rule: IsInvalid(reaction.UnicodeEmoji), Parameter: nameof(Reaction.UnicodeEmoji)),
                (Rule: IsInvalid(reaction.CreatedBy), Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsInvalid(reaction.UpdatedBy), Parameter: nameof(Reaction.UpdatedBy)),
                (Rule: IsInvalid(reaction.CreatedWhen), Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsInvalid(reaction.UpdatedWhen), Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsGreaterThan(reaction.Name, 30),
                    Parameter: nameof(Reaction.Name)),

                (Rule: IsGreaterThan(reaction.UnicodeEmoji, 16),
                    Parameter: nameof(Reaction.UnicodeEmoji)),

                (Rule: IsGreaterThan(reaction.CreatedBy, 255),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsGreaterThan(reaction.UpdatedBy, 255),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: reaction.UpdatedWhen,
                        secondDate: reaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: reaction.CreatedBy),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsNotSame(
                        first: reaction.UpdatedBy,
                        second: reaction.CreatedBy,
                        secondName: nameof(Reaction.CreatedBy)),
                    Parameter: nameof(Reaction.UpdatedBy)),

                // A row is contributed unpublished, and publication is the approve operation's to
                // grant (design §9.7.1 rules 1 and 3). Without these three rules any authenticated
                // caller can insert a row that is already Approved and IsPublished, which is public
                // the moment it lands — the approval workflow is simply skipped rather than bypassed.
                (Rule: IsSetOnAdd(reaction.IsPublished),
                    Parameter: nameof(Reaction.IsPublished)),

                (Rule: IsSetOnAdd(reaction.PublishDate),
                    Parameter: nameof(Reaction.PublishDate)),

                (Rule: IsNotContributableStatus(reaction.ApprovalStatus),
                    Parameter: nameof(Reaction.ApprovalStatus)),

                (Rule: await IsNotRecentAsync(reaction.CreatedWhen),
                    Parameter: nameof(Reaction.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyReactionAsync(
            Reaction reaction,
            SecurityContext securityContext)
        {
            ValidateReactionIsNotNull(reaction);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reaction.Id), Parameter: nameof(Reaction.Id)),
                (Rule: IsInvalid(reaction.Name), Parameter: nameof(Reaction.Name)),
                (Rule: IsInvalid(reaction.UnicodeEmoji), Parameter: nameof(Reaction.UnicodeEmoji)),
                (Rule: IsInvalid(reaction.CreatedBy), Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsInvalid(reaction.UpdatedBy), Parameter: nameof(Reaction.UpdatedBy)),
                (Rule: IsInvalid(reaction.CreatedWhen), Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsInvalid(reaction.UpdatedWhen), Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: IsGreaterThan(reaction.Name, 30),
                    Parameter: nameof(Reaction.Name)),

                (Rule: IsGreaterThan(reaction.UnicodeEmoji, 16),
                    Parameter: nameof(Reaction.UnicodeEmoji)),

                (Rule: IsGreaterThan(reaction.CreatedBy, 255),
                    Parameter: nameof(Reaction.CreatedBy)),

                (Rule: IsGreaterThan(reaction.UpdatedBy, 255),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: reaction.UpdatedBy),
                    Parameter: nameof(Reaction.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: reaction.UpdatedWhen,
                        secondDate: reaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(reaction.UpdatedWhen),
                    Parameter: nameof(Reaction.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateReactionEventEnvelopeAsync(
            EventEnvelope<Reaction> envelope,
            ReactionEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidReactionEventException(
                    message: "Invalid reaction event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Reaction)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidReactionEventException(
                    message: "Invalid reaction event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageReactionOnModify(
            Reaction inputReaction,
            Reaction storageReaction,
            bool mayTransitionApprovalStatus)
        {
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputReaction.CreatedWhen,
                        secondDate: storageReaction.CreatedWhen,
                        secondDateName: nameof(Reaction.CreatedWhen)),
                    Parameter: nameof(Reaction.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputReaction.CreatedBy,
                        second: storageReaction.CreatedBy,
                        secondName: nameof(Reaction.CreatedBy)),
                    Parameter: nameof(Reaction.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputReaction.UpdatedWhen,
                        secondDate: storageReaction.UpdatedWhen,
                        secondDateName: nameof(Reaction.UpdatedWhen)),
                    Parameter: nameof(Reaction.UpdatedWhen)),

                // The general modify is for content only. Every IApproval member belongs to the
                // approve operation (design §9.7.1 rules 2 and 3), so all five are pinned against
                // storage here — except the one carve-out: the owner or Publishers tier may move
                // the status between Draft and Submitted (§9.2). Without these pins any caller with
                // write permission could take a pending row and publish it through the general
                // modify, approving content nobody with authority over it ever looked at.
                (Rule: IsNotAPermittedStatusChangeOnModify(
                        inputStatus: inputReaction.ApprovalStatus,
                        storageStatus: storageReaction.ApprovalStatus,
                        mayTransition: mayTransitionApprovalStatus),
                    Parameter: nameof(Reaction.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: inputReaction.IsPublished,
                        second: storageReaction.IsPublished,
                        secondName: nameof(Reaction.IsPublished)),
                    Parameter: nameof(Reaction.IsPublished)),

                (Rule: IsNotSame(
                        firstDate: inputReaction.PublishDate,
                        secondDate: storageReaction.PublishDate,
                        secondDateName: nameof(Reaction.PublishDate)),
                    Parameter: nameof(Reaction.PublishDate)),

                // The bypass fields are derived on write and never carried on a general
                // modify: someone who bypass-approved could otherwise quietly clear the flag
                // that records it (design 9.7.1 rule 3). The reason is coalesced because a
                // null and an empty string are the same "no reason recorded".
                (Rule: IsNotSame(
                        first: inputReaction.IsApprovedByBypass,
                        second: storageReaction.IsApprovedByBypass,
                        secondName: nameof(Reaction.IsApprovedByBypass)),
                    Parameter: nameof(Reaction.IsApprovedByBypass)),

                (Rule: IsNotSame(
                        first: inputReaction.ApprovedByBypassReason ?? string.Empty,
                        second: storageReaction.ApprovedByBypassReason ?? string.Empty,
                        secondName: nameof(Reaction.ApprovedByBypassReason)),
                    Parameter: nameof(Reaction.ApprovedByBypassReason)));
        }

        private static void ValidateOnRetrieveReactionById(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveReactionById(Guid reactionId, string? deletionReason) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(Reaction.DeletionReason)));

        private static void ValidateOnHardRemoveReactionById(Guid reactionId) =>
            Validate(
                message: "Reaction is invalid, fix the errors and try again.",
                (Rule: IsInvalid(reactionId), Parameter: nameof(Reaction.Id)));

        private static void ValidateStorageReaction(Reaction maybeReaction, Guid reactionId)
        {
            if (maybeReaction is null)
            {
                throw new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");
            }
        }

        private static void ValidateReactionIsNotNull(Reaction reaction)
        {
            if (reaction is null)
            {
                throw new NullReactionException(message: "Reaction is null.");
            }
        }

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            string first,
            string second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Text is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
            };

        private static dynamic IsNotSame(
            bool first,
            bool second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset? firstDate,
            DateTimeOffset? secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsSetOnAdd(bool value) => new
        {
            Condition = value,
            Message = "Value is not allowed on add"
        };

        private static dynamic IsSetOnAdd(DateTimeOffset? date) => new
        {
            Condition = date is not null,
            Message = "Date is not allowed on add"
        };

        // a caller may save work in progress or submit it for review; the remaining states are
        // verdicts, and a verdict is the approval workflow's to record (design §9.7.1 rule 1)
        private static dynamic IsNotContributableStatus(ApprovalStatus approvalStatus) => new
        {
            Condition = approvalStatus != ApprovalStatus.Draft
                && approvalStatus != ApprovalStatus.Submitted,

            Message = $"Value must be {nameof(ApprovalStatus.Draft)} " +
                $"or {nameof(ApprovalStatus.Submitted)} on add"
        };

        // The one carve-out on modify (design §9.2 rules 4-6): the owner or Publishers tier may
        // move the status between Draft and Submitted, because submitting is inseparable from the
        // edit that made the work ready. Everything else about the status stays pinned, and the
        // caller must have been found eligible before this is reached — a reviewer holds write
        // permission on the row and must still never move the status (HR-3).
        private static dynamic IsNotAPermittedStatusChangeOnModify(
            ApprovalStatus inputStatus,
            ApprovalStatus storageStatus,
            bool mayTransition) => new
            {
                Condition =
                    inputStatus != storageStatus
                        && (mayTransition is false
                            || IsDraftOrSubmitted(inputStatus) is false
                            || IsDraftOrSubmitted(storageStatus) is false),

                Message = "Value is not the same as storage approval status"
            };

        private static bool IsDraftOrSubmitted(ApprovalStatus approvalStatus) =>
            approvalStatus == ApprovalStatus.Draft
                || approvalStatus == ApprovalStatus.Submitted;

        private async ValueTask<dynamic> IsNotRecentAsync(DateTimeOffset date)
        {
            var (isNotRecent, startDate, endDate) = await IsDateNotRecentAsync(date);

            return new
            {
                Condition = isNotRecent,
                Message = $"Date is not recent. Expected a value between {startDate} and {endDate} but found {date}"
            };
        }

        private async ValueTask<(bool IsNotRecent, DateTimeOffset StartDate, DateTimeOffset EndDate)>
            IsDateNotRecentAsync(DateTimeOffset date)
        {
            int pastThreshold = 90;
            int futureThreshold = 0;
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            DateTimeOffset startDate = currentDateTime.AddSeconds(-pastThreshold);
            DateTimeOffset endDate = currentDateTime.AddSeconds(futureThreshold);
            bool isNotRecent = date < startDate || date > endDate;

            return (isNotRecent, startDate, endDate);
        }

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidReactionException = new InvalidReactionException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidReactionException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidReactionException.ThrowIfContainsErrors();
        }
    }
}
