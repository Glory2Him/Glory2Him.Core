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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    public partial class ApprovalService
    {
        private async ValueTask ValidateOnAddApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.CreatedBy),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsNotSame(
                        first: approval.UpdatedBy,
                        second: approval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approval.CreatedWhen),
                    Parameter: nameof(Approval.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.UpdatedBy),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approval.UpdatedWhen),
                    Parameter: nameof(Approval.UpdatedWhen)));
        }

        private static void ValidateApprovalEventEnvelope(EventEnvelope<Approval> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalEventException(
                    message: "Invalid approval event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageApprovalOnModify(
            Approval inputApproval,
            Approval storageApproval)
        {
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApproval.CreatedWhen,
                        secondDate: storageApproval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApproval.CreatedBy,
                        second: storageApproval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApproval.UpdatedWhen,
                        secondDate: storageApproval.UpdatedWhen,
                        secondDateName: nameof(Approval.UpdatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateOnRemoveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateOnHardRemoveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateStorageApproval(Approval maybeApproval, Guid approvalId)
        {
            if (maybeApproval is null)
            {
                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }
        }

        private static void ValidateApprovalIsNotNull(Approval approval)
        {
            if (approval is null)
            {
                throw new NullApprovalException(message: "Approval is null.");
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
            var invalidApprovalException = new InvalidApprovalException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalException.ThrowIfContainsErrors();
        }
    }
}
