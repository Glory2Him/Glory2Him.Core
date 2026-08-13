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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Links
{
    internal partial class LinkService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.LinkReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is blocked from contributing links.");
            }
        }

        // the moderation roles that may act on and read non-public versions for review and
        // audit (Reviewer, Publisher, Admin — global or Link-scoped, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.LinkReviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.LinkPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // the publisher tier: the roles the approve operation itself requires, and the only ones
        // besides the owner that may move a submission status through the general modify. Strictly
        // narrower than the review tier — a Reviewer is absent by design (§8.6 HR-3).
        private static bool HasPublisherRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.LinkPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower process rules (approved items fork, only the latest version is amended)
        // stay in the orchestration, which needs owner writes to approved rows for the
        // version fork and role writes for the publish flip.
        //
        // Returns whether the caller may also use the Draft <-> Submitted carve-out (§9.2): the
        // owner or the Publisher tier. It falls out of the ownership check already performed, so
        // it is returned rather than recomputed. A Reviewer holds write permission but is NOT in
        // the publisher tier, so it may amend content and still never move the status (HR-3).
        private async ValueTask<bool> ValidateUserCanModifyStorageLinkAsync(
            Link storageLink,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageLink.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is not allowed to modify this link.");
            }

            return isOwner || HasPublisherRole(securityContext);
        }

        // removing content is a takedown, not a moderation step — the owner may remove
        // their own link and an Admin may remove anyone's; Reviewers and Publishers
        // moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageLinkAsync(
            Link storageLink,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageLink.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is not allowed to remove this link.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveLink(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly)
                || securityContext.Roles.Contains(Roles.LinkReadOnly))
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is blocked from contributing links.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedLinkException(
                    message: "The current user is not allowed to permanently remove this link.");
            }
        }

        private async ValueTask ValidateOnAddLinkAsync(
            Link link,
            SecurityContext securityContext)
        {
            ValidateLinkIsNotNull(link);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Id), Parameter: nameof(Link.Id)),
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)),
                (Rule: IsInvalid(link.GroupId), Parameter: nameof(Link.GroupId)),
                (Rule: IsInvalid(link.CreatedBy), Parameter: nameof(Link.CreatedBy)),
                (Rule: IsInvalid(link.UpdatedBy), Parameter: nameof(Link.UpdatedBy)),
                (Rule: IsInvalid(link.CreatedWhen), Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsInvalid(link.UpdatedWhen), Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsGreaterThan(link.Name, 255),
                    Parameter: nameof(Link.Name)),

                (Rule: IsGreaterThan(link.Url, 2048),
                    Parameter: nameof(Link.Url)),

                (Rule: IsGreaterThan(link.LinkType, 64),
                    Parameter: nameof(Link.LinkType)),

                (Rule: IsGreaterThan(link.CreatedBy, 255),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsGreaterThan(link.UpdatedBy, 255),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: link.UpdatedWhen,
                        secondDate: link.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: link.CreatedBy),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsNotSame(
                        first: link.UpdatedBy,
                        second: link.CreatedBy,
                        secondName: nameof(Link.CreatedBy)),
                    Parameter: nameof(Link.UpdatedBy)),

                // A row is contributed unpublished, and publication is the approve operation's to
                // grant (design §9.7.1 rules 1 and 3). Without these three rules any authenticated
                // caller can insert a row that is already Approved and IsPublished, which is public
                // the moment it lands — the approval workflow is simply skipped rather than bypassed.
                (Rule: IsSetOnAdd(link.IsPublished),
                    Parameter: nameof(Link.IsPublished)),

                (Rule: IsSetOnAdd(link.PublishDate),
                    Parameter: nameof(Link.PublishDate)),

                (Rule: IsNotContributableStatus(link.ApprovalStatus),
                    Parameter: nameof(Link.ApprovalStatus)),

                (Rule: await IsNotRecentAsync(link.CreatedWhen),
                    Parameter: nameof(Link.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyLinkAsync(
            Link link,
            SecurityContext securityContext)
        {
            ValidateLinkIsNotNull(link);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Id), Parameter: nameof(Link.Id)),
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)),
                (Rule: IsInvalid(link.GroupId), Parameter: nameof(Link.GroupId)),
                (Rule: IsInvalid(link.CreatedBy), Parameter: nameof(Link.CreatedBy)),
                (Rule: IsInvalid(link.UpdatedBy), Parameter: nameof(Link.UpdatedBy)),
                (Rule: IsInvalid(link.CreatedWhen), Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsInvalid(link.UpdatedWhen), Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsGreaterThan(link.Name, 255),
                    Parameter: nameof(Link.Name)),

                (Rule: IsGreaterThan(link.Url, 2048),
                    Parameter: nameof(Link.Url)),

                (Rule: IsGreaterThan(link.LinkType, 64),
                    Parameter: nameof(Link.LinkType)),

                (Rule: IsGreaterThan(link.CreatedBy, 255),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsGreaterThan(link.UpdatedBy, 255),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: link.UpdatedBy),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: link.UpdatedWhen,
                        secondDate: link.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(link.UpdatedWhen),
                    Parameter: nameof(Link.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateLinkEventEnvelopeAsync(
            EventEnvelope<Link> envelope,
            LinkEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidLinkEventException(
                    message: "Invalid link event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Link)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidLinkEventException(
                    message: "Invalid link event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageLinkOnModify(
            Link inputLink,
            Link storageLink,
            bool mayTransitionApprovalStatus)
        {
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputLink.CreatedWhen,
                        secondDate: storageLink.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputLink.CreatedBy,
                        second: storageLink.CreatedBy,
                        secondName: nameof(Link.CreatedBy)),
                    Parameter: nameof(Link.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputLink.UpdatedWhen,
                        secondDate: storageLink.UpdatedWhen,
                        secondDateName: nameof(Link.UpdatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)),

                // The general modify is for content only. Every IApproval member belongs to the
                // approve operation (design §9.7.1 rules 2 and 3), so all five are pinned against
                // storage here — except the one carve-out: the owner or Publisher tier may move
                // the status between Draft and Submitted (§9.2). Without these pins any caller with
                // write permission could take a pending row and publish it through the general
                // modify, approving content nobody with authority over it ever looked at.
                (Rule: IsNotAPermittedStatusChangeOnModify(
                        inputStatus: inputLink.ApprovalStatus,
                        storageStatus: storageLink.ApprovalStatus,
                        mayTransition: mayTransitionApprovalStatus),
                    Parameter: nameof(Link.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: inputLink.IsPublished,
                        second: storageLink.IsPublished,
                        secondName: nameof(Link.IsPublished)),
                    Parameter: nameof(Link.IsPublished)),

                (Rule: IsNotSame(
                        firstDate: inputLink.PublishDate,
                        secondDate: storageLink.PublishDate,
                        secondDateName: nameof(Link.PublishDate)),
                    Parameter: nameof(Link.PublishDate)),

                // The bypass fields are derived on write and never carried on a general
                // modify: someone who bypass-approved could otherwise quietly clear the flag
                // that records it (design 9.7.1 rule 3). The reason is coalesced because a
                // null and an empty string are the same "no reason recorded".
                (Rule: IsNotSame(
                        first: inputLink.IsApprovedByBypass,
                        second: storageLink.IsApprovedByBypass,
                        secondName: nameof(Link.IsApprovedByBypass)),
                    Parameter: nameof(Link.IsApprovedByBypass)),

                (Rule: IsNotSame(
                        first: inputLink.ApprovedByBypassReason ?? string.Empty,
                        second: storageLink.ApprovedByBypassReason ?? string.Empty,
                        secondName: nameof(Link.ApprovedByBypassReason)),
                    Parameter: nameof(Link.ApprovedByBypassReason)));
        }

        private static void ValidateOnRetrieveLinkById(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveLinkById(Guid linkId, string? deletionReason) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(Link.DeletionReason)));

        private static void ValidateOnHardRemoveLinkById(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static void ValidateStorageLink(Link maybeLink, Guid linkId)
        {
            if (maybeLink is null)
            {
                throw new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");
            }
        }

        private static void ValidateLinkIsNotNull(Link link)
        {
            if (link is null)
            {
                throw new NullLinkException(message: "Link is null.");
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

        // The one carve-out on modify (design §9.2 rules 4-6): the owner or Publisher tier may
        // move the status between Draft and Submitted, because submitting is inseparable from the
        // edit that made the work ready. Everything else about the status stays pinned, and the
        // caller must have been found eligible before this is reached — a Reviewer holds write
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
            var invalidLinkException = new InvalidLinkException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidLinkException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidLinkException.ThrowIfContainsErrors();
        }
    }
}
