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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ContentItemReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is blocked from contributing content items.");
            }
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateContentItemEventEnvelopeAsync(
            EventEnvelope<ContentItem> envelope,
            ContentItemEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ContentItem)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidContentItemEventException(
                    message: "Invalid content item event. Integrity verification failed.");
            }
        }

        // ContentItem is the one entity type with three role tiers rather than two, because
        // it is the only one carrying a ContentType (design §18.6 rule 5). The tiers widen
        // from narrow to broad — ContentItem-Story-Reviewer ⊂ ContentItem-Reviewer ⊂ Reviewer
        // — and rule 4 binds both directions: holding ANY of them satisfies a check for that
        // content type, and the narrow role NEVER satisfies a check for a different one. Both
        // halves are load-bearing, so the checks below are always asked about a content type.

        // the broad tiers, which cover every content type at once and so need no per-row
        // question asked of them
        private static bool HasBroadReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.ContentItemReviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.ContentItemPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // the narrow tier: authority over one content type and never over another
        private static bool HasContentTypeReviewRole(
            SecurityContext securityContext,
            ContentType contentType) =>
            securityContext.Roles.Contains(
                    Roles.ReviewerFor(EntityType.ContentItem, contentType))
                || securityContext.Roles.Contains(
                    Roles.PublisherFor(EntityType.ContentItem, contentType));

        // the moderation roles that may act on and read non-public versions of THIS content
        // type for review and audit (§16.6, §18.6)
        private static bool HasReviewRole(
            SecurityContext securityContext,
            ContentType contentType) =>
            HasBroadReviewRole(securityContext)
                || HasContentTypeReviewRole(securityContext, contentType);

        // The content types a narrow-tier caller may review. A collection filter is a
        // queryable predicate and cannot call a role check per row, so the caller's narrow
        // grants are resolved once, here, into a set the predicate can test membership of.
        private static ContentType[] ReviewableContentTypes(SecurityContext securityContext) =>
            Enum.GetValues<ContentType>()
                .Where(contentType => HasContentTypeReviewRole(securityContext, contentType))
                .ToArray();

        // the publisher tier: the roles the dedicated approve operation itself requires, and
        // the only ones besides the owner that may move a submission status through modify.
        // Strictly narrower than the review tier — a Reviewer is absent by design (§8.6 HR-3).
        private static bool HasPublisherRole(
            SecurityContext securityContext,
            ContentType contentType) =>
            securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.ContentItemPublisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Contains(
                    Roles.PublisherFor(EntityType.ContentItem, contentType));

        // row-level write permission: the owner or a review role may write the row — the
        // narrower process rules (approved items fork, only the latest version is amended)
        // stay in the orchestration, which needs owner writes to approved rows for the
        // version fork and role writes for the publish flip
        //
        // Returns whether the caller may also use the Draft <-> Submitted carve-out (design
        // §9.2 rules 4-6). The answer falls out of the ownership check this method already
        // performs, so it is returned rather than recomputed - a second GetUserIdAsync would
        // be a wasted call and a second chance for the two answers to disagree.
        //
        // Note what the carve-out is NOT gated on: write permission. A Reviewer passes the
        // check below and may amend content, and must still never move an approval status
        // (§8.6 HR-3).
        private async ValueTask<bool> ValidateUserCanModifyStorageContentItemAsync(
            ContentItem storageContentItem,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageContentItem.CreatedBy == actorUserId;

            if (isOwner is false
                && HasReviewRole(securityContext, storageContentItem.ContentType) is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to modify this content item.");
            }

            return isOwner
                || HasPublisherRole(securityContext, storageContentItem.ContentType);
        }

        // Approved and Rejected are TERMINAL: the content of a row in either state is immutable
        // in place, to its owner, to a Publisher and to an Admin alike (§3.4 rules 7 and 16,
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
        // A ContentItem is Versioned, so the amendment is not lost — it becomes a new version.
        // That fork belongs to ContentItemProcessingService (§10.17 rule 2, §12.4.1), which
        // reaches the terminal row first and writes a new one rather than calling this. The
        // refusal here is what makes the fork the ONLY route: an exposer may bind straight to
        // the foundation, and a rule enforced only above it is not enforced (§8.6.1).
        //
        // The §9.2 Draft <-> Submitted carve-out is unreachable from here and stays that way:
        // it is only ever reached from Draft or Submitted, so a terminal row is refused before
        // the carve-out is consulted.
        private static void ValidateStorageContentItemIsNotTerminal(
            ContentItem storageContentItem)
        {
            bool isTerminal =
                storageContentItem.ApprovalStatus == ApprovalStatus.Approved
                    || storageContentItem.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTerminal)
            {
                throw new InvalidContentItemException(
                    message: "Content item cannot be modified from status " +
                        $"{storageContentItem.ApprovalStatus}.");
            }
        }

        // removing content is a takedown, not a moderation step — the owner may remove
        // their own item and an Admin may remove anyone's; Reviewers and Publishers
        // moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageContentItemAsync(
            ContentItem storageContentItem,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageContentItem.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to remove this content item.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveContentItem(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly)
                || securityContext.Roles.Contains(Roles.ContentItemReadOnly))
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is blocked from contributing content items.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to permanently remove this content item.");
            }
        }

        private async ValueTask ValidateOnAddContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateContentItemIsNotNull(contentItem);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentType), Parameter: nameof(ContentItem.ContentType)),
                (Rule: IsInvalid(contentItem.GroupId), Parameter: nameof(ContentItem.GroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItem.CreatedBy, 255),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsGreaterThan(contentItem.UpdatedBy, 255),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: contentItem.UpdatedWhen,
                        secondDate: contentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItem.CreatedBy),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsNotSame(
                        first: contentItem.UpdatedBy,
                        second: contentItem.CreatedBy,
                        secondName: nameof(ContentItem.CreatedBy)),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                // An item is contributed unpublished, and publication is the approve
                // operation's to grant (design §9.7.1 rules 1 and 3). Without these three
                // rules any authenticated caller can insert a row that is already Approved
                // and IsPublished, which is public the moment it lands — the approval
                // workflow is simply skipped rather than bypassed. The orchestration's add
                // already forces the first two onto the new row, but it is not the gate:
                // this operation has its own event address (§8.6.1, §14.6).
                (Rule: IsSetOnAdd(contentItem.IsPublished),
                    Parameter: nameof(ContentItem.IsPublished)),

                (Rule: IsSetOnAdd(contentItem.PublishDate),
                    Parameter: nameof(ContentItem.PublishDate)),

                (Rule: IsNotContributableStatus(contentItem.ApprovalStatus),
                    Parameter: nameof(ContentItem.ApprovalStatus)),

                (Rule: await IsNotRecentAsync(contentItem.CreatedWhen),
                    Parameter: nameof(ContentItem.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateContentItemIsNotNull(contentItem);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentType), Parameter: nameof(ContentItem.ContentType)),
                (Rule: IsInvalid(contentItem.GroupId), Parameter: nameof(ContentItem.GroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItem.CreatedBy, 255),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsGreaterThan(contentItem.UpdatedBy, 255),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItem.UpdatedBy),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: contentItem.UpdatedWhen,
                        secondDate: contentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(contentItem.UpdatedWhen),
                    Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateOnCheckContentItemContentExists(
            ContentType contentType,
            string contentHash) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentType), Parameter: nameof(ContentItem.ContentType)),
                (Rule: IsInvalid(contentHash), Parameter: nameof(ContentItem.ContentHash)));

        private static void ValidateOnRetrieveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveContentItemById(
            Guid contentItemId,
            string? deletionReason) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(ContentItem.DeletionReason)));

        private static void ValidateOnHardRemoveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateStorageContentItem(ContentItem maybeContentItem, Guid contentItemId)
        {
            if (maybeContentItem is null)
            {
                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }
        }

        private static void ValidateAgainstStorageContentItemOnModify(
            ContentItem inputContentItem,
            ContentItem storageContentItem,
            bool mayTransitionApprovalStatus)
        {
            Validate(
                message: "Content item is invalid, fix the errors and try again.",

                (Rule: IsNotSame(
                        firstDate: inputContentItem.CreatedWhen,
                        secondDate: storageContentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.CreatedWhen)),

                (Rule: IsNotSame(
                        first: inputContentItem.CreatedBy,
                        second: storageContentItem.CreatedBy,
                        secondName: nameof(ContentItem.CreatedBy)),
                    Parameter: nameof(ContentItem.CreatedBy)),

                // The content type is create-only (design §12.4.1 rule 7a). Each type carries
                // its own validation rules and composes content-type-scoped role names
                // (§18.6), so relabelling would move an item into a type its content was never
                // checked against and whose reviewers never saw it.
                (Rule: IsNotSame(
                        first: inputContentItem.ContentType,
                        second: storageContentItem.ContentType,
                        secondName: nameof(ContentItem.ContentType)),
                    Parameter: nameof(ContentItem.ContentType)),

                // The version lineage is how an approved item's history is read back. Left
                // writable, a caller could detach an item from its group or crown an older
                // version as latest, and the version anyone actually reviewed would be gone.
                // The orchestration mints these on the fork; modify never carries them.
                (Rule: IsNotSame(
                        first: inputContentItem.GroupId,
                        second: storageContentItem.GroupId,
                        secondName: nameof(ContentItem.GroupId)),
                    Parameter: nameof(ContentItem.GroupId)),

                (Rule: IsNotSame(
                        first: inputContentItem.Version,
                        second: storageContentItem.Version,
                        secondName: nameof(ContentItem.Version)),
                    Parameter: nameof(ContentItem.Version)),

                (Rule: IsNotSame(
                        first: inputContentItem.IsLatestVersion,
                        second: storageContentItem.IsLatestVersion,
                        secondName: nameof(ContentItem.IsLatestVersion)),
                    Parameter: nameof(ContentItem.IsLatestVersion)),

                // The general modify is for content only. Every IApproval member belongs to the
                // approve operation (design §9.7.1 rules 2 and 3), so they are pinned here
                // rather than carried — otherwise reaching approved and published on the
                // primary content entity would need no review role, no publisher tier, no
                // access decision and no approval conditions, only write permission on the row.
                //
                // Pinning is by comparison against storage, not by omission (§9.7.1): default
                // is a legal value for most of these — Draft is 0, false is the default for
                // both flags — so a rule that trusted absence could not tell "not supplied"
                // from "set to the dangerous value".
                (Rule: IsNotAPermittedStatusChangeOnModify(
                        inputStatus: inputContentItem.ApprovalStatus,
                        storageStatus: storageContentItem.ApprovalStatus,
                        mayTransition: mayTransitionApprovalStatus),
                    Parameter: nameof(ContentItem.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: inputContentItem.IsPublished,
                        second: storageContentItem.IsPublished,
                        secondName: nameof(ContentItem.IsPublished)),
                    Parameter: nameof(ContentItem.IsPublished)),

                (Rule: IsNotSame(
                        firstDate: inputContentItem.PublishDate,
                        secondDate: storageContentItem.PublishDate,
                        secondDateName: nameof(ContentItem.PublishDate)),
                    Parameter: nameof(ContentItem.PublishDate)),

                // The bypass record is pinned hardest of all, because it is the only field here
                // whose whole purpose is to be read back later as evidence. The approve
                // operation derives it from the access decision and never accepts it; leaving
                // it writable through modify would hand it back to the caller by the side door
                // — someone who bypass-approved could then quietly clear the flag that says so.
                (Rule: IsNotSame(
                        first: inputContentItem.IsApprovedByBypass,
                        second: storageContentItem.IsApprovedByBypass,
                        secondName: nameof(ContentItem.IsApprovedByBypass)),
                    Parameter: nameof(ContentItem.IsApprovedByBypass)),

                // Coalesced because the column is nullable and "no reason recorded" is the same
                // fact whether it is stored as null or as empty — a caller sending one for the
                // other is not attempting a change worth refusing.
                (Rule: IsNotSame(
                        first: inputContentItem.ApprovedByBypassReason ?? string.Empty,
                        second: storageContentItem.ApprovedByBypassReason ?? string.Empty,
                        secondName: nameof(ContentItem.ApprovedByBypassReason)),
                    Parameter: nameof(ContentItem.ApprovedByBypassReason)),

                (Rule: IsSame(
                        firstDate: inputContentItem.UpdatedWhen,
                        secondDate: storageContentItem.UpdatedWhen,
                        secondDateName: nameof(ContentItem.UpdatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemException(message: "Content item is null.");
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

        // structural validation for an enum crossing a boundary — rejects an out-of-range
        // value (e.g. a stale client sending a since-removed member); it cannot detect
        // "caller forgot to set it", since ContentType has no unset sentinel
        private static dynamic IsInvalid(ContentType contentType) => new
        {
            Condition = Enum.IsDefined(contentType) == false,
            Message = "Value is not a supported content type"
        };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = IsExceedingLength(text, maxLength),
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private static bool IsExceedingLength(string? text, int maxLength) =>
            (text ?? string.Empty).Length > maxLength;

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
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
            DateTimeOffset? firstDate,
            DateTimeOffset? secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsNotSame(
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
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
            int first,
            int second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            ContentType first,
            ContentType second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        // The one carve-out on modify (design §9.2 rules 4-6): an eligible caller may move the
        // status between Draft and Submitted, because submitting is inseparable from the edit
        // that made the work ready. Everything else about the status stays pinned, and the
        // caller must have been found eligible before this is reached — a Reviewer holds write
        // permission on the row and must still never move the status (§8.6 HR-3).
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

        // a caller may save work in progress or submit it for review; the remaining states
        // are verdicts, and a verdict is the approval workflow's to record (design §9.7.1
        // rule 1)
        private static dynamic IsNotContributableStatus(ApprovalStatus approvalStatus) => new
        {
            Condition = IsDraftOrSubmitted(approvalStatus) is false,

            Message = $"Value must be {nameof(ApprovalStatus.Draft)} " +
                $"or {nameof(ApprovalStatus.Submitted)} on add"
        };

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
            };

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
            var invalidContentItemException = new InvalidContentItemException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemException.ThrowIfContainsErrors();
        }
    }
}
