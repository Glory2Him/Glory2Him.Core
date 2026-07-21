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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleService
    {
        private async ValueTask ValidateOnAddApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingRoleIsNotNull(approvalSettingRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingRole.Id), Parameter: nameof(ApprovalSettingRole.Id)),
                (Rule: IsInvalid(approvalSettingRole.RoleName), Parameter: nameof(ApprovalSettingRole.RoleName)),

                (Rule: IsInvalid(approvalSettingRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingRole.CreatedBy), Parameter: nameof(ApprovalSettingRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingRole.UpdatedBy), Parameter: nameof(ApprovalSettingRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingRole.CreatedWhen), Parameter: nameof(ApprovalSettingRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingRole.UpdatedWhen), Parameter: nameof(ApprovalSettingRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingRole.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalSettingRole.UpdatedWhen,
                        secondDate: approvalSettingRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingRole.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingRole.CreatedBy),
                    Parameter: nameof(ApprovalSettingRole.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalSettingRole.UpdatedBy,
                        second: approvalSettingRole.CreatedBy,
                        secondName: nameof(ApprovalSettingRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingRole.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalSettingRole.CreatedWhen),
                    Parameter: nameof(ApprovalSettingRole.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalSettingRoleAsync(
            ApprovalSettingRole approvalSettingRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingRoleIsNotNull(approvalSettingRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingRole.Id), Parameter: nameof(ApprovalSettingRole.Id)),
                (Rule: IsInvalid(approvalSettingRole.RoleName), Parameter: nameof(ApprovalSettingRole.RoleName)),

                (Rule: IsInvalid(approvalSettingRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingRole.CreatedBy), Parameter: nameof(ApprovalSettingRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingRole.UpdatedBy), Parameter: nameof(ApprovalSettingRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingRole.CreatedWhen), Parameter: nameof(ApprovalSettingRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingRole.UpdatedWhen), Parameter: nameof(ApprovalSettingRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingRole.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingRole.UpdatedBy),
                    Parameter: nameof(ApprovalSettingRole.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalSettingRole.UpdatedWhen,
                        secondDate: approvalSettingRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingRole.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalSettingRole.UpdatedWhen),
                    Parameter: nameof(ApprovalSettingRole.UpdatedWhen)));
        }

        private static void ValidateApprovalSettingRoleEventEnvelope(EventEnvelope<ApprovalSettingRole> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalSettingRoleEventException(
                    message: "Invalid approval setting role event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageApprovalSettingRoleOnModify(
            ApprovalSettingRole inputApprovalSettingRole,
            ApprovalSettingRole storageApprovalSettingRole)
        {
            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalSettingRole.CreatedWhen,
                        secondDate: storageApprovalSettingRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingRole.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalSettingRole.CreatedBy,
                        second: storageApprovalSettingRole.CreatedBy,
                        secondName: nameof(ApprovalSettingRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingRole.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApprovalSettingRole.UpdatedWhen,
                        secondDate: storageApprovalSettingRole.UpdatedWhen,
                        secondDateName: nameof(ApprovalSettingRole.UpdatedWhen)),
                    Parameter: nameof(ApprovalSettingRole.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalSettingRoleById(Guid approvalSettingRoleId) =>
            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingRoleId), Parameter: nameof(ApprovalSettingRole.Id)));

        private static void ValidateOnRemoveApprovalSettingRoleById(Guid approvalSettingRoleId) =>
            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingRoleId), Parameter: nameof(ApprovalSettingRole.Id)));

        private static void ValidateOnHardRemoveApprovalSettingRoleById(Guid approvalSettingRoleId) =>
            Validate(
                message: "Approval setting role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingRoleId), Parameter: nameof(ApprovalSettingRole.Id)));

        private static void ValidateStorageApprovalSettingRole(ApprovalSettingRole maybeApprovalSettingRole, Guid approvalSettingRoleId)
        {
            if (maybeApprovalSettingRole is null)
            {
                throw new NotFoundApprovalSettingRoleException(
                    message: $"Approval setting role not found with id: {approvalSettingRoleId}.");
            }
        }

        private static void ValidateApprovalSettingRoleIsNotNull(ApprovalSettingRole approvalSettingRole)
        {
            if (approvalSettingRole is null)
            {
                throw new NullApprovalSettingRoleException(message: "Approval setting role is null.");
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
            var invalidApprovalSettingRoleException = new InvalidApprovalSettingRoleException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalSettingRoleException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalSettingRoleException.ThrowIfContainsErrors();
        }
    }
}
