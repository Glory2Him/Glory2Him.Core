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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles
{
    internal partial class ApprovalSettingReviewerRoleService
    {
        private async ValueTask ValidateOnAddApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingReviewerRoleIsNotNull(approvalSettingReviewerRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingReviewerRole.Id), Parameter: nameof(ApprovalSettingReviewerRole.Id)),
                (Rule: IsInvalid(approvalSettingReviewerRole.RoleName), Parameter: nameof(ApprovalSettingReviewerRole.RoleName)),

                (Rule: IsInvalid(approvalSettingReviewerRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingReviewerRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingReviewerRole.CreatedBy), Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingReviewerRole.UpdatedBy), Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingReviewerRole.CreatedWhen), Parameter: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingReviewerRole.UpdatedWhen), Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalSettingReviewerRole.UpdatedWhen,
                        secondDate: approvalSettingReviewerRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingReviewerRole.CreatedBy),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalSettingReviewerRole.UpdatedBy,
                        second: approvalSettingReviewerRole.CreatedBy,
                        secondName: nameof(ApprovalSettingReviewerRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalSettingReviewerRole.CreatedWhen),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalSettingReviewerRoleAsync(
            ApprovalSettingReviewerRole approvalSettingReviewerRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingReviewerRoleIsNotNull(approvalSettingReviewerRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingReviewerRole.Id), Parameter: nameof(ApprovalSettingReviewerRole.Id)),
                (Rule: IsInvalid(approvalSettingReviewerRole.RoleName), Parameter: nameof(ApprovalSettingReviewerRole.RoleName)),

                (Rule: IsInvalid(approvalSettingReviewerRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingReviewerRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingReviewerRole.CreatedBy), Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingReviewerRole.UpdatedBy), Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingReviewerRole.CreatedWhen), Parameter: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingReviewerRole.UpdatedWhen), Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingReviewerRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingReviewerRole.UpdatedBy),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalSettingReviewerRole.UpdatedWhen,
                        secondDate: approvalSettingReviewerRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalSettingReviewerRole.UpdatedWhen),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)));
        }

        private static void ValidateApprovalSettingReviewerRoleEventEnvelope(EventEnvelope<ApprovalSettingReviewerRole> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalSettingReviewerRoleEventException(
                    message: "Invalid approval setting reviewer role event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageApprovalSettingReviewerRoleOnModify(
            ApprovalSettingReviewerRole inputApprovalSettingReviewerRole,
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole)
        {
            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalSettingReviewerRole.CreatedWhen,
                        secondDate: storageApprovalSettingReviewerRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalSettingReviewerRole.CreatedBy,
                        second: storageApprovalSettingReviewerRole.CreatedBy,
                        secondName: nameof(ApprovalSettingReviewerRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingReviewerRole.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApprovalSettingReviewerRole.UpdatedWhen,
                        secondDate: storageApprovalSettingReviewerRole.UpdatedWhen,
                        secondDateName: nameof(ApprovalSettingReviewerRole.UpdatedWhen)),
                    Parameter: nameof(ApprovalSettingReviewerRole.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalSettingReviewerRoleById(Guid approvalSettingReviewerRoleId) =>
            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingReviewerRoleId), Parameter: nameof(ApprovalSettingReviewerRole.Id)));

        private static void ValidateOnRemoveApprovalSettingReviewerRoleById(Guid approvalSettingReviewerRoleId) =>
            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingReviewerRoleId), Parameter: nameof(ApprovalSettingReviewerRole.Id)));

        private static void ValidateOnHardRemoveApprovalSettingReviewerRoleById(Guid approvalSettingReviewerRoleId) =>
            Validate(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingReviewerRoleId), Parameter: nameof(ApprovalSettingReviewerRole.Id)));

        private static void ValidateStorageApprovalSettingReviewerRole(ApprovalSettingReviewerRole maybeApprovalSettingReviewerRole, Guid approvalSettingReviewerRoleId)
        {
            if (maybeApprovalSettingReviewerRole is null)
            {
                throw new NotFoundApprovalSettingReviewerRoleException(
                    message: $"Approval setting reviewer role not found with id: {approvalSettingReviewerRoleId}.");
            }
        }

        private static void ValidateApprovalSettingReviewerRoleIsNotNull(ApprovalSettingReviewerRole approvalSettingReviewerRole)
        {
            if (approvalSettingReviewerRole is null)
            {
                throw new NullApprovalSettingReviewerRoleException(message: "Approval setting reviewer role is null.");
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
            var invalidApprovalSettingReviewerRoleException = new InvalidApprovalSettingReviewerRoleException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalSettingReviewerRoleException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalSettingReviewerRoleException.ThrowIfContainsErrors();
        }
    }
}
