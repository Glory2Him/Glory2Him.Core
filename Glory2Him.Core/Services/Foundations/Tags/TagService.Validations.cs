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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial class TagService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.TagReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is blocked from contributing tags.");
            }
        }

        // the moderation roles that may act on and read non-public versions for review and
        // audit (Reviewers, Publishers, Administrators — global or Tag-scoped, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewers)
                || securityContext.Roles.Contains(Roles.TagReviewers)
                || securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.TagPublishers)
                || securityContext.Roles.Contains(Roles.Administrators);

        // the publisher tier: the roles the approve operation itself requires, and the only ones
        // besides the owner that may move a submission status through the general modify. Strictly
        // narrower than the review tier — a reviewer is absent by design (§8.6 HR-3).
        private static bool HasPublisherRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.TagPublishers)
                || securityContext.Roles.Contains(Roles.Administrators);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower process rules stay in the orchestration (§14.6 altitude split).
        //
        // Returns whether the caller may also use the Draft <-> Submitted carve-out (§9.2): the
        // owner or the Publishers tier. It falls out of the ownership check already performed, so
        // it is returned rather than recomputed. A reviewer holds write permission but is NOT in
        // the publisher tier, so it may amend content and still never move the status (HR-3).
        private async ValueTask<bool> ValidateUserCanModifyStorageTagAsync(
            Tag storageTag,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageTag.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to modify this tag.");
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
        // A tag never forks, so refusing the write IS the enforcement — there is no version to
        // amend into, and the only way back is the Administrators override on the approval transition
        // (§8.6 HR-4). For the Versioned entities an orchestration turns this same condition
        // into a fork instead (§10.17, #199).
        //
        // The §9.2 Draft <-> Submitted carve-out is unreachable from here and stays that way:
        // it is only ever reached from Draft or Submitted, so a terminal row is refused before
        // the carve-out is consulted.
        private static void ValidateStorageTagIsNotTerminal(Tag storageTag)
        {
            bool isTerminal =
                storageTag.ApprovalStatus == ApprovalStatus.Approved
                    || storageTag.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTerminal)
            {
                throw new InvalidTagException(
                    message: "Tag cannot be modified from status " +
                        $"{storageTag.ApprovalStatus}.");
            }
        }

        // removing a tag is a takedown, not a moderation step — the owner may remove
        // their own tag and an administrator may remove anyone's; Reviewers and Publishers
        // moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageTagAsync(
            Tag storageTag,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageTag.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to remove this tag.");
            }
        }

        // a hard remove destroys the row and its audit trail — Administrators only
        private static void ValidateUserCanHardRemoveTag(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly)
                || securityContext.Roles.Contains(Roles.TagReadOnly))
            {
                throw new UnauthorizedTagException(
                    message: "The current user is blocked from contributing tags.");
            }

            if (securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to permanently remove this tag.");
            }
        }

        private async ValueTask ValidateOnAddTagAsync(
            Tag tag,
            SecurityContext securityContext)
        {
            ValidateTagIsNotNull(tag);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tag.Id), Parameter: nameof(Tag.Id)),
                (Rule: IsInvalid(tag.Name), Parameter: nameof(Tag.Name)),
                (Rule: IsInvalid(tag.CreatedBy), Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsInvalid(tag.UpdatedBy), Parameter: nameof(Tag.UpdatedBy)),
                (Rule: IsInvalid(tag.CreatedWhen), Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsInvalid(tag.UpdatedWhen), Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsGreaterThan(tag.Name, 30),
                    Parameter: nameof(Tag.Name)),

                (Rule: IsGreaterThan(tag.CreatedBy, 255),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsGreaterThan(tag.UpdatedBy, 255),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: tag.UpdatedWhen,
                        secondDate: tag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: tag.CreatedBy),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsNotSame(
                        first: tag.UpdatedBy,
                        second: tag.CreatedBy,
                        secondName: nameof(Tag.CreatedBy)),
                    Parameter: nameof(Tag.UpdatedBy)),

                // A row is contributed unpublished, and publication is the approve operation's to
                // grant (design §9.7.1 rules 1 and 3). Without these three rules any authenticated
                // caller can insert a row that is already Approved and IsPublished, which is public
                // the moment it lands — the approval workflow is simply skipped rather than bypassed.
                (Rule: IsSetOnAdd(tag.IsPublished),
                    Parameter: nameof(Tag.IsPublished)),

                (Rule: IsSetOnAdd(tag.PublishDate),
                    Parameter: nameof(Tag.PublishDate)),

                (Rule: IsNotContributableStatus(tag.ApprovalStatus),
                    Parameter: nameof(Tag.ApprovalStatus)),

                (Rule: await IsNotRecentAsync(tag.CreatedWhen),
                    Parameter: nameof(Tag.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyTagAsync(
            Tag tag,
            SecurityContext securityContext)
        {
            ValidateTagIsNotNull(tag);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tag.Id), Parameter: nameof(Tag.Id)),
                (Rule: IsInvalid(tag.Name), Parameter: nameof(Tag.Name)),
                (Rule: IsInvalid(tag.CreatedBy), Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsInvalid(tag.UpdatedBy), Parameter: nameof(Tag.UpdatedBy)),
                (Rule: IsInvalid(tag.CreatedWhen), Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsInvalid(tag.UpdatedWhen), Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsGreaterThan(tag.Name, 30),
                    Parameter: nameof(Tag.Name)),

                (Rule: IsGreaterThan(tag.CreatedBy, 255),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsGreaterThan(tag.UpdatedBy, 255),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: tag.UpdatedBy),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: tag.UpdatedWhen,
                        secondDate: tag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(tag.UpdatedWhen),
                    Parameter: nameof(Tag.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateTagEventEnvelopeAsync(
            EventEnvelope<Tag> envelope,
            TagEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidTagEventException(
                    message: "Invalid tag event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Tag)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidTagEventException(
                    message: "Invalid tag event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageTagOnModify(
            Tag inputTag,
            Tag storageTag,
            bool mayTransitionApprovalStatus)
        {
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputTag.CreatedWhen,
                        secondDate: storageTag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputTag.CreatedBy,
                        second: storageTag.CreatedBy,
                        secondName: nameof(Tag.CreatedBy)),
                    Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputTag.UpdatedWhen,
                        secondDate: storageTag.UpdatedWhen,
                        secondDateName: nameof(Tag.UpdatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)),

                // The general modify is for content only. Every IApproval member belongs to the
                // approve operation (design §9.7.1 rules 2 and 3), so all five are pinned against
                // storage here — except the one carve-out: the owner or Publishers tier may move
                // the status between Draft and Submitted (§9.2). Without these pins any caller with
                // write permission could take a pending row and publish it through the general
                // modify, approving content nobody with authority over it ever looked at.
                (Rule: IsNotAPermittedStatusChangeOnModify(
                        inputStatus: inputTag.ApprovalStatus,
                        storageStatus: storageTag.ApprovalStatus,
                        mayTransition: mayTransitionApprovalStatus),
                    Parameter: nameof(Tag.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: inputTag.IsPublished,
                        second: storageTag.IsPublished,
                        secondName: nameof(Tag.IsPublished)),
                    Parameter: nameof(Tag.IsPublished)),

                (Rule: IsNotSame(
                        firstDate: inputTag.PublishDate,
                        secondDate: storageTag.PublishDate,
                        secondDateName: nameof(Tag.PublishDate)),
                    Parameter: nameof(Tag.PublishDate)),

                // The bypass fields are derived on write and never carried on a general
                // modify: someone who bypass-approved could otherwise quietly clear the flag
                // that records it (design 9.7.1 rule 3). The reason is coalesced because a
                // null and an empty string are the same "no reason recorded".
                (Rule: IsNotSame(
                        first: inputTag.IsApprovedByBypass,
                        second: storageTag.IsApprovedByBypass,
                        secondName: nameof(Tag.IsApprovedByBypass)),
                    Parameter: nameof(Tag.IsApprovedByBypass)),

                (Rule: IsNotSame(
                        first: inputTag.ApprovedByBypassReason ?? string.Empty,
                        second: storageTag.ApprovedByBypassReason ?? string.Empty,
                        secondName: nameof(Tag.ApprovedByBypassReason)),
                    Parameter: nameof(Tag.ApprovedByBypassReason)));
        }

        private static void ValidateOnRetrieveTagById(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveTagById(Guid tagId, string? deletionReason) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(Tag.DeletionReason)));

        private static void ValidateOnHardRemoveTagById(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        private static void ValidateStorageTag(Tag maybeTag, Guid tagId)
        {
            if (maybeTag is null)
            {
                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
            }
        }

        private static void ValidateTagIsNotNull(Tag tag)
        {
            if (tag is null)
            {
                throw new NullTagException(message: "Tag is null.");
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
            var invalidTagException = new InvalidTagException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidTagException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidTagException.ThrowIfContainsErrors();
        }
    }
}
