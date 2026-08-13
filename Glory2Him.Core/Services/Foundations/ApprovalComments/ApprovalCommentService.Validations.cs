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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial class ApprovalCommentService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        // §16.6 spells an entity-scoped role "{Entity}-Reviewer" / "{Entity}-Publisher"
        private const string ScopedReviewerRoleSuffix = Roles.ReviewerSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublisherSuffix;

        // commenting on a review thread is a conversation, not a moderation step: the
        // submitter answers the reviewer's questions on their own submission, so any
        // authenticated caller who is not globally blocked may comment. No entity-scoped
        // ReadOnly role exists for approval workflow records, so only the global one blocks.
        private static void ValidateUserIsAllowedToComment(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is blocked from contributing approval comments.");
            }
        }

        // the review roles that may act on and read approval workflow records (Reviewer,
        // Publisher, Admin). Approval comments carry no entity-scoped roles of their own, so
        // by the §16.6 convention any "{Entity}-Reviewer"/"{Entity}-Publisher" role counts:
        // the comment row alone does not say which entity type the approval targets, so the
        // foundation cannot tell a Tag-Reviewer's comment thread from a Link-Reviewer's.
        // Narrowing a scoped reviewer to the approvals of their own entity type is an
        // orchestration concern, where the approval and its target are read together.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // row-level write permission: the author may edit their own comment and a review
        // role may write it too — reviewers flip IsResolved on a submitter's comment as the
        // thread is worked through
        private async ValueTask ValidateUserCanModifyStorageApprovalCommentAsync(
            ApprovalComment storageApprovalComment,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalComment.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to modify this approval comment.");
            }
        }

        // removing a comment retracts it from the review record — the author may retract
        // their own and an Admin may retract anyone's; reviewers resolve threads instead
        private async ValueTask ValidateUserCanRemoveStorageApprovalCommentAsync(
            ApprovalComment storageApprovalComment,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalComment.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to remove this approval comment.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveApprovalComment(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is blocked from contributing approval comments.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to permanently remove this approval comment.");
            }
        }

        private async ValueTask ValidateOnAddApprovalCommentAsync(
            ApprovalComment approvalComment,
            SecurityContext securityContext)
        {
            ValidateApprovalCommentIsNotNull(approvalComment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalComment.Id), Parameter: nameof(ApprovalComment.Id)),
                (Rule: IsInvalid(approvalComment.ApprovalId), Parameter: nameof(ApprovalComment.ApprovalId)),
                (Rule: IsInvalid(approvalComment.UserId), Parameter: nameof(ApprovalComment.UserId)),
                (Rule: IsInvalid(approvalComment.CreatedBy), Parameter: nameof(ApprovalComment.CreatedBy)),
                (Rule: IsInvalid(approvalComment.UpdatedBy), Parameter: nameof(ApprovalComment.UpdatedBy)),
                (Rule: IsInvalid(approvalComment.CreatedWhen), Parameter: nameof(ApprovalComment.CreatedWhen)),
                (Rule: IsInvalid(approvalComment.UpdatedWhen), Parameter: nameof(ApprovalComment.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalComment.CreatedBy, 255),
                    Parameter: nameof(ApprovalComment.CreatedBy)),

                (Rule: IsGreaterThan(approvalComment.UpdatedBy, 255),
                    Parameter: nameof(ApprovalComment.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalComment.UpdatedWhen,
                        secondDate: approvalComment.CreatedWhen,
                        secondDateName: nameof(ApprovalComment.CreatedWhen)),
                    Parameter: nameof(ApprovalComment.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalComment.CreatedBy),
                    Parameter: nameof(ApprovalComment.CreatedBy)),

                // The commenter is the caller, not a label the caller chooses. Comments became
                // load-bearing for the approval gate once
                // RequireReviewCommentResolutionBeforeApprovals settled: that setting blocks
                // approval until every ApprovalComment.IsResolved is true, so a comment
                // attributed to someone else moves a gate rather than merely misreporting who
                // spoke.
                //
                // Bound rather than stamped, matching how every other actor fact in this
                // codebase is handled: a caller who meant to attribute the comment elsewhere
                // gets the mismatch back by name instead of a silent re-attribution.
                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalComment.UserId),
                    Parameter: nameof(ApprovalComment.UserId)),

                (Rule: IsNotSame(
                        first: approvalComment.UpdatedBy,
                        second: approvalComment.CreatedBy,
                        secondName: nameof(ApprovalComment.CreatedBy)),
                    Parameter: nameof(ApprovalComment.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalComment.CreatedWhen),
                    Parameter: nameof(ApprovalComment.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            SecurityContext securityContext)
        {
            ValidateApprovalCommentIsNotNull(approvalComment);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalComment.Id), Parameter: nameof(ApprovalComment.Id)),
                (Rule: IsInvalid(approvalComment.ApprovalId), Parameter: nameof(ApprovalComment.ApprovalId)),
                (Rule: IsInvalid(approvalComment.UserId), Parameter: nameof(ApprovalComment.UserId)),
                (Rule: IsInvalid(approvalComment.CreatedBy), Parameter: nameof(ApprovalComment.CreatedBy)),
                (Rule: IsInvalid(approvalComment.UpdatedBy), Parameter: nameof(ApprovalComment.UpdatedBy)),
                (Rule: IsInvalid(approvalComment.CreatedWhen), Parameter: nameof(ApprovalComment.CreatedWhen)),
                (Rule: IsInvalid(approvalComment.UpdatedWhen), Parameter: nameof(ApprovalComment.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalComment.CreatedBy, 255),
                    Parameter: nameof(ApprovalComment.CreatedBy)),

                (Rule: IsGreaterThan(approvalComment.UpdatedBy, 255),
                    Parameter: nameof(ApprovalComment.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalComment.UpdatedBy),
                    Parameter: nameof(ApprovalComment.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalComment.UpdatedWhen,
                        secondDate: approvalComment.CreatedWhen,
                        secondDateName: nameof(ApprovalComment.CreatedWhen)),
                    Parameter: nameof(ApprovalComment.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalComment.UpdatedWhen),
                    Parameter: nameof(ApprovalComment.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateApprovalCommentEventEnvelopeAsync(
            EventEnvelope<ApprovalComment> envelope,
            ApprovalCommentEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ApprovalComment)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageApprovalCommentOnModify(
            ApprovalComment inputApprovalComment,
            ApprovalComment storageApprovalComment)
        {
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalComment.CreatedWhen,
                        secondDate: storageApprovalComment.CreatedWhen,
                        secondDateName: nameof(ApprovalComment.CreatedWhen)),
                    Parameter: nameof(ApprovalComment.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalComment.CreatedBy,
                        second: storageApprovalComment.CreatedBy,
                        secondName: nameof(ApprovalComment.CreatedBy)),
                    Parameter: nameof(ApprovalComment.CreatedBy)),

                // Both are fixed at add. Pinned against STORAGE rather than against the caller
                // because a review-role caller or an Admin may legitimately amend someone
                // else's comment (ValidateUserCanModifyStorageApprovalCommentAsync) — but
                // correcting the text must not mean moving the comment onto another person's
                // name, or onto a different approval. Re-pointing ApprovalId would walk an
                // unresolved comment off the approval it is blocking, which is the gate
                // RequireReviewCommentResolutionBeforeApprovals exists to hold shut.
                (Rule: IsNotSame(
                        first: inputApprovalComment.UserId,
                        second: storageApprovalComment.UserId,
                        secondName: nameof(ApprovalComment.UserId)),
                    Parameter: nameof(ApprovalComment.UserId)),

                (Rule: IsNotSame(
                        first: inputApprovalComment.ApprovalId,
                        second: storageApprovalComment.ApprovalId,
                        secondName: nameof(ApprovalComment.ApprovalId)),
                    Parameter: nameof(ApprovalComment.ApprovalId)),

                (Rule: IsSame(
                        firstDate: inputApprovalComment.UpdatedWhen,
                        secondDate: storageApprovalComment.UpdatedWhen,
                        secondDateName: nameof(ApprovalComment.UpdatedWhen)),
                    Parameter: nameof(ApprovalComment.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalCommentById(Guid approvalCommentId) =>
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalCommentId), Parameter: nameof(ApprovalComment.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveApprovalCommentById(
            Guid approvalCommentId,
            string? deletionReason) =>
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalCommentId), Parameter: nameof(ApprovalComment.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(ApprovalComment.DeletionReason)));

        private static void ValidateOnHardRemoveApprovalCommentById(Guid approvalCommentId) =>
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalCommentId), Parameter: nameof(ApprovalComment.Id)));

        private static void ValidateStorageApprovalComment(ApprovalComment maybeApprovalComment, Guid approvalCommentId)
        {
            if (maybeApprovalComment is null)
            {
                throw new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");
            }
        }

        private static void ValidateApprovalCommentIsNotNull(ApprovalComment approvalComment)
        {
            if (approvalComment is null)
            {
                throw new NullApprovalCommentException(message: "Approval comment is null.");
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
            var invalidApprovalCommentException = new InvalidApprovalCommentException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalCommentException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalCommentException.ThrowIfContainsErrors();
        }
    }
}
