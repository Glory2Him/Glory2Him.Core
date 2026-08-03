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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles
{
    internal partial class ApprovalSettingPublisherRoleService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        // approval setting publisher roles are policy configuration authored by
        // administrators only — a single Admin gate covers Add, Modify and Remove
        private static void ValidateUserIsAllowedToAdministerApprovalSettingPublisherRoles(
            SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalSettingPublisherRoleException(
                    message: "The current user is not authenticated.");
            }

            if (HasAdminRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalSettingPublisherRoleException(
                    message: "The current user is not allowed to administer approval setting publisher roles.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only, same as
        // every other approval setting publisher role write
        private static void ValidateUserCanHardRemoveApprovalSettingPublisherRole(SecurityContext securityContext) =>
            ValidateUserIsAllowedToAdministerApprovalSettingPublisherRoles(securityContext);

        // the only role that may write policy configuration — reads need no role at all,
        // just an authenticated caller, so submitters can see the approval rules
        private static bool HasAdminRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Admin);

        private async ValueTask ValidateOnAddApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingPublisherRoleIsNotNull(approvalSettingPublisherRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingPublisherRole.Id), Parameter: nameof(ApprovalSettingPublisherRole.Id)),
                (Rule: IsInvalid(approvalSettingPublisherRole.RoleName), Parameter: nameof(ApprovalSettingPublisherRole.RoleName)),

                (Rule: IsInvalid(approvalSettingPublisherRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingPublisherRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingPublisherRole.CreatedBy), Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingPublisherRole.UpdatedBy), Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingPublisherRole.CreatedWhen), Parameter: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingPublisherRole.UpdatedWhen), Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalSettingPublisherRole.UpdatedWhen,
                        secondDate: approvalSettingPublisherRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingPublisherRole.CreatedBy),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalSettingPublisherRole.UpdatedBy,
                        second: approvalSettingPublisherRole.CreatedBy,
                        secondName: nameof(ApprovalSettingPublisherRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalSettingPublisherRole.CreatedWhen),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingPublisherRoleIsNotNull(approvalSettingPublisherRole);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingPublisherRole.Id), Parameter: nameof(ApprovalSettingPublisherRole.Id)),
                (Rule: IsInvalid(approvalSettingPublisherRole.RoleName), Parameter: nameof(ApprovalSettingPublisherRole.RoleName)),

                (Rule: IsInvalid(approvalSettingPublisherRole.ApprovalSettingId),
                    Parameter: nameof(ApprovalSettingPublisherRole.ApprovalSettingId)),

                (Rule: IsInvalid(approvalSettingPublisherRole.CreatedBy), Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),
                (Rule: IsInvalid(approvalSettingPublisherRole.UpdatedBy), Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),
                (Rule: IsInvalid(approvalSettingPublisherRole.CreatedWhen), Parameter: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                (Rule: IsInvalid(approvalSettingPublisherRole.UpdatedWhen), Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.RoleName, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.RoleName)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.CreatedBy, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),

                (Rule: IsGreaterThan(approvalSettingPublisherRole.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSettingPublisherRole.UpdatedBy),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalSettingPublisherRole.UpdatedWhen,
                        secondDate: approvalSettingPublisherRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalSettingPublisherRole.UpdatedWhen),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)));
        }

        private static void ValidateApprovalSettingPublisherRoleEventEnvelope(EventEnvelope<ApprovalSettingPublisherRole> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalSettingPublisherRoleEventException(
                    message: "Invalid approval setting publisher role event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageApprovalSettingPublisherRoleOnModify(
            ApprovalSettingPublisherRole inputApprovalSettingPublisherRole,
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole)
        {
            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalSettingPublisherRole.CreatedWhen,
                        secondDate: storageApprovalSettingPublisherRole.CreatedWhen,
                        secondDateName: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalSettingPublisherRole.CreatedBy,
                        second: storageApprovalSettingPublisherRole.CreatedBy,
                        secondName: nameof(ApprovalSettingPublisherRole.CreatedBy)),
                    Parameter: nameof(ApprovalSettingPublisherRole.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApprovalSettingPublisherRole.UpdatedWhen,
                        secondDate: storageApprovalSettingPublisherRole.UpdatedWhen,
                        secondDateName: nameof(ApprovalSettingPublisherRole.UpdatedWhen)),
                    Parameter: nameof(ApprovalSettingPublisherRole.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalSettingPublisherRoleById(Guid approvalSettingPublisherRoleId) =>
            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingPublisherRoleId), Parameter: nameof(ApprovalSettingPublisherRole.Id)));

        private static void ValidateOnRemoveApprovalSettingPublisherRoleById(Guid approvalSettingPublisherRoleId) =>
            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingPublisherRoleId), Parameter: nameof(ApprovalSettingPublisherRole.Id)));

        private static void ValidateOnHardRemoveApprovalSettingPublisherRoleById(Guid approvalSettingPublisherRoleId) =>
            Validate(
                message: "Approval setting publisher role is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingPublisherRoleId), Parameter: nameof(ApprovalSettingPublisherRole.Id)));

        private static void ValidateStorageApprovalSettingPublisherRole(ApprovalSettingPublisherRole maybeApprovalSettingPublisherRole, Guid approvalSettingPublisherRoleId)
        {
            if (maybeApprovalSettingPublisherRole is null)
            {
                throw new NotFoundApprovalSettingPublisherRoleException(
                    message: $"Approval setting publisher role not found with id: {approvalSettingPublisherRoleId}.");
            }
        }

        private static void ValidateApprovalSettingPublisherRoleIsNotNull(ApprovalSettingPublisherRole approvalSettingPublisherRole)
        {
            if (approvalSettingPublisherRole is null)
            {
                throw new NullApprovalSettingPublisherRoleException(message: "Approval setting publisher role is null.");
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
            var invalidApprovalSettingPublisherRoleException = new InvalidApprovalSettingPublisherRoleException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalSettingPublisherRoleException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalSettingPublisherRoleException.ThrowIfContainsErrors();
        }
    }
}
