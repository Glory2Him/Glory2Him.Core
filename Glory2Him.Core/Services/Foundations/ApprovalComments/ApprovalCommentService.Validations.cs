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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial class ApprovalCommentService
    {
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

        private static void ValidateApprovalCommentEventEnvelope(EventEnvelope<ApprovalComment> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. " +
                        "The event envelope, its content and metadata are required.");
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

        private static void ValidateOnRemoveApprovalCommentById(Guid approvalCommentId) =>
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalCommentId), Parameter: nameof(ApprovalComment.Id)));

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
