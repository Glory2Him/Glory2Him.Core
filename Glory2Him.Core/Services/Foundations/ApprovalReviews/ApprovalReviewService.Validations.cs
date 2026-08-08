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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial class ApprovalReviewService
    {
        // the §16.6 scoped-role suffixes; the entity prefix in front of them varies per
        // entity type, so only the suffix is a fixed part of the convention
        private const string ScopedReviewerRoleSuffix = Roles.ReviewerSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublisherSuffix;

        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not authenticated.");
            }

            // an approval review is workflow bookkeeping rather than user-contributed
            // content, so no ApprovalReview-scoped ReadOnly role exists — only the global
            // block role applies here
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is blocked from contributing approval reviews.");
            }
        }

        // recording a verdict IS the review act (§8.9), so adding a review demands a review
        // role on top of the contribution gate — a submitter may be reviewed, never review
        private static void ValidateUserIsAllowedToReviewApprovals(SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);

            if (HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to review approvals.");
            }
        }

        // the review roles that may record and read verdicts: the global Reviewer,
        // Publisher and Admin roles plus — by the §16.6 naming convention — any
        // entity-scoped "%EntityType%-Reviewer"/"%EntityType%-Publisher" role. The
        // approval review row names no entity type, so the foundation cannot tell a
        // Tag-Reviewer's verdict from a Link-Reviewer's one row-locally; narrowing a
        // reviewer to the entity type they actually review is an orchestration concern,
        // which reaches the approval and the item under review
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // row-level write permission: a review is the reviewer's own verdict, so only its
        // author may amend it — another reviewer records their own review instead; an
        // Admin may correct anyone's for support and moderation
        private async ValueTask ValidateUserCanModifyStorageApprovalReviewAsync(
            ApprovalReview storageApprovalReview,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalReview.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to modify this approval review.");
            }
        }

        // withdrawing a review is the author's own retraction — the owner may remove their
        // verdict and an Admin may remove anyone's; other reviewers cannot erase a peer's
        private async ValueTask ValidateUserCanRemoveStorageApprovalReviewAsync(
            ApprovalReview storageApprovalReview,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalReview.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to remove this approval review.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveApprovalReview(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to permanently remove this approval review.");
            }
        }

        private async ValueTask ValidateOnAddApprovalReviewAsync(
            ApprovalReview approvalReview,
            SecurityContext securityContext)
        {
            ValidateApprovalReviewIsNotNull(approvalReview);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReview.Id), Parameter: nameof(ApprovalReview.Id)),
                (Rule: IsInvalid(approvalReview.ApprovalId), Parameter: nameof(ApprovalReview.ApprovalId)),
                (Rule: IsInvalid(approvalReview.ReviewerId), Parameter: nameof(ApprovalReview.ReviewerId)),
                (Rule: IsInvalid(approvalReview.CreatedBy), Parameter: nameof(ApprovalReview.CreatedBy)),
                (Rule: IsInvalid(approvalReview.UpdatedBy), Parameter: nameof(ApprovalReview.UpdatedBy)),
                (Rule: IsInvalid(approvalReview.CreatedWhen), Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsInvalid(approvalReview.UpdatedWhen), Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalReview.ReviewerId, 450),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsGreaterThan(approvalReview.CreatedBy, 255),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                (Rule: IsGreaterThan(approvalReview.UpdatedBy, 255),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalReview.UpdatedWhen,
                        secondDate: approvalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReview.CreatedBy),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                // The reviewer is the caller, not a label the caller chooses. The only thing
                // standing behind design §7.7 rule 1 is
                // UX_ApprovalReviews_ApprovalId_ReviewerId, so an unbound ReviewerId leaves
                // that rule with nothing: one reviewer files three verdicts under three
                // invented ids, clears the index each time, and meets
                // RequiredNumberOfApprovals = 3 alone. A threshold met by one person is not
                // a threshold.
                //
                // Note the index is UNFILTERED — no predicate on StatusId or IsDeleted — so
                // what it actually enforces is one review per reviewer per approval EVER,
                // which is stricter than §7.7 rule 1's "one ACTIVE review". That gap belongs
                // to the index, not to this rule, but it becomes reachable once this binding
                // lands: §7.7 rule 7 lets a reviewer re-file after dismissal, and the INSERT
                // that needs will now collide. Filtering the index is the fix; it is recorded
                // in §7.7 rather than done here.
                //
                // Bound rather than stamped, matching how every other actor fact in this
                // codebase is handled: a caller who meant to attribute the review elsewhere
                // gets the mismatch back by name instead of a silent re-attribution.
                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReview.ReviewerId),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsNotSame(
                        first: approvalReview.UpdatedBy,
                        second: approvalReview.CreatedBy,
                        secondName: nameof(ApprovalReview.CreatedBy)),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                // A review IS a verdict, so the set it may carry is closed to the two a
                // reviewer can reach (design §7.7 rule 2). StatusId was previously unvalidated
                // altogether, so an undefined enum value could be persisted and counted.
                //
                // Dismissed is excluded deliberately: §9.5 makes dismissal something that
                // HAPPENS to a review when an entity-scoped change invalidates it, not
                // something its author declares. A reviewer who could dismiss their own review
                // could retract a rejection without recording a verdict, which is the same
                // outcome as changing it but leaves no trace of the change.
                (Rule: IsNotAReviewVerdict(approvalReview.StatusId),
                    Parameter: nameof(ApprovalReview.StatusId)),

                (Rule: await IsNotRecentAsync(approvalReview.CreatedWhen),
                    Parameter: nameof(ApprovalReview.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            SecurityContext securityContext)
        {
            ValidateApprovalReviewIsNotNull(approvalReview);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReview.Id), Parameter: nameof(ApprovalReview.Id)),
                (Rule: IsInvalid(approvalReview.ApprovalId), Parameter: nameof(ApprovalReview.ApprovalId)),
                (Rule: IsInvalid(approvalReview.ReviewerId), Parameter: nameof(ApprovalReview.ReviewerId)),
                (Rule: IsInvalid(approvalReview.CreatedBy), Parameter: nameof(ApprovalReview.CreatedBy)),
                (Rule: IsInvalid(approvalReview.UpdatedBy), Parameter: nameof(ApprovalReview.UpdatedBy)),
                (Rule: IsInvalid(approvalReview.CreatedWhen), Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsInvalid(approvalReview.UpdatedWhen), Parameter: nameof(ApprovalReview.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalReview.ReviewerId, 450),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsGreaterThan(approvalReview.CreatedBy, 255),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                (Rule: IsGreaterThan(approvalReview.UpdatedBy, 255),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalReview.UpdatedBy),
                    Parameter: nameof(ApprovalReview.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalReview.UpdatedWhen,
                        secondDate: approvalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)),

                // Changing a verdict is legitimate — a reviewer who raised a concern and had
                // it answered on the ApprovalComment thread should move their own Rejected to
                // Approved rather than the approval needing a bypass. What a reviewer may not
                // do is declare their review Dismissed, for the same reason as on add.
                (Rule: IsNotAReviewVerdict(approvalReview.StatusId),
                    Parameter: nameof(ApprovalReview.StatusId)),

                (Rule: await IsNotRecentAsync(approvalReview.UpdatedWhen),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)));
        }

        private static void ValidateApprovalReviewEventEnvelope(EventEnvelope<ApprovalReview> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        // A dismissed review is closed. §9.5 retains it for audit and lets the reviewer file a
        // NEW one; §7.7 rule 1 says decisions are not superseded or replaced. Editing the
        // dismissed row instead would rewrite history in place and, because dismissal is what
        // records that a review no longer describes the current content, would silently
        // re-attach a stale verdict to amended text.
        private static void ValidateStorageApprovalReviewIsNotDismissed(
            ApprovalReview storageApprovalReview)
        {
            if (storageApprovalReview.StatusId == ApprovalStatus.Dismissed)
            {
                throw new InvalidApprovalReviewException(
                    message: "A dismissed approval review cannot be amended. " +
                        "Submit a new review instead.");
            }
        }

        private static void ValidateAgainstStorageApprovalReviewOnModify(
            ApprovalReview inputApprovalReview,
            ApprovalReview storageApprovalReview)
        {
            ValidateStorageApprovalReviewIsNotDismissed(storageApprovalReview);

            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalReview.CreatedWhen,
                        secondDate: storageApprovalReview.CreatedWhen,
                        secondDateName: nameof(ApprovalReview.CreatedWhen)),
                    Parameter: nameof(ApprovalReview.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalReview.CreatedBy,
                        second: storageApprovalReview.CreatedBy,
                        secondName: nameof(ApprovalReview.CreatedBy)),
                    Parameter: nameof(ApprovalReview.CreatedBy)),

                // Both halves of UX_ApprovalReviews_ApprovalId_ReviewerId are fixed at add.
                // Pinned against STORAGE rather than against the caller because an Admin may
                // legitimately amend anyone's review (ValidateUserCanModifyStorageApprovalReviewAsync)
                // — but correcting a verdict must not mean moving it onto another reviewer's
                // name, or onto a different approval, either of which walks the row past the
                // uniqueness rule that makes §7.7 rule 1 mean anything.
                (Rule: IsNotSame(
                        first: inputApprovalReview.ReviewerId,
                        second: storageApprovalReview.ReviewerId,
                        secondName: nameof(ApprovalReview.ReviewerId)),
                    Parameter: nameof(ApprovalReview.ReviewerId)),

                (Rule: IsNotSame(
                        first: inputApprovalReview.ApprovalId,
                        second: storageApprovalReview.ApprovalId,
                        secondName: nameof(ApprovalReview.ApprovalId)),
                    Parameter: nameof(ApprovalReview.ApprovalId)),

                (Rule: IsSame(
                        firstDate: inputApprovalReview.UpdatedWhen,
                        secondDate: storageApprovalReview.UpdatedWhen,
                        secondDateName: nameof(ApprovalReview.UpdatedWhen)),
                    Parameter: nameof(ApprovalReview.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateOnRemoveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateOnHardRemoveApprovalReviewById(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        private static void ValidateStorageApprovalReview(ApprovalReview maybeApprovalReview, Guid approvalReviewId)
        {
            if (maybeApprovalReview is null)
            {
                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }
        }

        private static void ValidateApprovalReviewIsNotNull(ApprovalReview approvalReview)
        {
            if (approvalReview is null)
            {
                throw new NullApprovalReviewException(message: "Approval review is null.");
            }
        }

        // A review carries a verdict, and only the two a reviewer can reach. Draft and
        // Submitted are entity states, not review outcomes; Dismissed is what happens TO a
        // review when an entity-scoped change invalidates it (design §9.5), never something
        // its author declares. Undefined enum values are refused here too — StatusId is
        // persisted as an int, so nothing else stops one.
        private static dynamic IsNotAReviewVerdict(ApprovalStatus statusId) => new
        {
            Condition =
                statusId != ApprovalStatus.Approved
                    && statusId != ApprovalStatus.Rejected,

            Message = $"Value must be {nameof(ApprovalStatus.Approved)} " +
                $"or {nameof(ApprovalStatus.Rejected)}"
        };

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
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsGreaterThan(string text, int maxLength) => new
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
            var invalidApprovalReviewException = new InvalidApprovalReviewException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalReviewException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalReviewException.ThrowIfContainsErrors();
        }
    }
}
