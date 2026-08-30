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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettings
{
    internal partial class ApprovalSettingService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        // approval settings are admin policy configuration — every write (add, modify,
        // remove, hard remove) is Administrators only
        private static void ValidateUserIsAllowedToAdministerApprovalSettings(
            SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalSettingException(
                    message: "The current user is not authenticated.");
            }

            // the global ReadOnly ban precedes the Administrators check, so a banned administrator cannot reach
            // past it — this gate is every approval setting write, hard remove included.
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalSettingException(
                    message: "The current user is blocked from administering approval settings.");
            }

            if (securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedApprovalSettingException(
                    message: "The current user is not allowed to administer approval settings.");
            }
        }

        private async ValueTask ValidateOnAddApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingIsNotNull(approvalSetting);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSetting.Id), Parameter: nameof(ApprovalSetting.Id)),
                (Rule: IsInvalid(approvalSetting.CreatedBy), Parameter: nameof(ApprovalSetting.CreatedBy)),
                (Rule: IsInvalid(approvalSetting.UpdatedBy), Parameter: nameof(ApprovalSetting.UpdatedBy)),
                (Rule: IsInvalid(approvalSetting.CreatedWhen), Parameter: nameof(ApprovalSetting.CreatedWhen)),
                (Rule: IsInvalid(approvalSetting.UpdatedWhen), Parameter: nameof(ApprovalSetting.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSetting.CreatedBy, 255),
                    Parameter: nameof(ApprovalSetting.CreatedBy)),

                (Rule: IsGreaterThan(approvalSetting.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSetting.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: approvalSetting.UpdatedWhen,
                        secondDate: approvalSetting.CreatedWhen,
                        secondDateName: nameof(ApprovalSetting.CreatedWhen)),
                    Parameter: nameof(ApprovalSetting.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSetting.CreatedBy),
                    Parameter: nameof(ApprovalSetting.CreatedBy)),

                (Rule: IsNotSame(
                        first: approvalSetting.UpdatedBy,
                        second: approvalSetting.CreatedBy,
                        secondName: nameof(ApprovalSetting.CreatedBy)),
                    Parameter: nameof(ApprovalSetting.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approvalSetting.CreatedWhen),
                    Parameter: nameof(ApprovalSetting.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalSettingAsync(
            ApprovalSetting approvalSetting,
            SecurityContext securityContext)
        {
            ValidateApprovalSettingIsNotNull(approvalSetting);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSetting.Id), Parameter: nameof(ApprovalSetting.Id)),
                (Rule: IsInvalid(approvalSetting.CreatedBy), Parameter: nameof(ApprovalSetting.CreatedBy)),
                (Rule: IsInvalid(approvalSetting.UpdatedBy), Parameter: nameof(ApprovalSetting.UpdatedBy)),
                (Rule: IsInvalid(approvalSetting.CreatedWhen), Parameter: nameof(ApprovalSetting.CreatedWhen)),
                (Rule: IsInvalid(approvalSetting.UpdatedWhen), Parameter: nameof(ApprovalSetting.UpdatedWhen)),

                (Rule: IsGreaterThan(approvalSetting.CreatedBy, 255),
                    Parameter: nameof(ApprovalSetting.CreatedBy)),

                (Rule: IsGreaterThan(approvalSetting.UpdatedBy, 255),
                    Parameter: nameof(ApprovalSetting.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approvalSetting.UpdatedBy),
                    Parameter: nameof(ApprovalSetting.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approvalSetting.UpdatedWhen,
                        secondDate: approvalSetting.CreatedWhen,
                        secondDateName: nameof(ApprovalSetting.CreatedWhen)),
                    Parameter: nameof(ApprovalSetting.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approvalSetting.UpdatedWhen),
                    Parameter: nameof(ApprovalSetting.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateApprovalSettingEventEnvelopeAsync(
            EventEnvelope<ApprovalSetting> envelope,
            ApprovalSettingEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ApprovalSetting)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageApprovalSettingOnModify(
            ApprovalSetting inputApprovalSetting,
            ApprovalSetting storageApprovalSetting)
        {
            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApprovalSetting.CreatedWhen,
                        secondDate: storageApprovalSetting.CreatedWhen,
                        secondDateName: nameof(ApprovalSetting.CreatedWhen)),
                    Parameter: nameof(ApprovalSetting.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApprovalSetting.CreatedBy,
                        second: storageApprovalSetting.CreatedBy,
                        secondName: nameof(ApprovalSetting.CreatedBy)),
                    Parameter: nameof(ApprovalSetting.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApprovalSetting.UpdatedWhen,
                        secondDate: storageApprovalSetting.UpdatedWhen,
                        secondDateName: nameof(ApprovalSetting.UpdatedWhen)),
                    Parameter: nameof(ApprovalSetting.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveApprovalSettingById(Guid approvalSettingId) =>
            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingId), Parameter: nameof(ApprovalSetting.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveApprovalSettingById(
            Guid approvalSettingId,
            string? deletionReason) =>
            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingId), Parameter: nameof(ApprovalSetting.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(ApprovalSetting.DeletionReason)));

        private static void ValidateOnHardRemoveApprovalSettingById(Guid approvalSettingId) =>
            Validate(
                message: "Approval setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalSettingId), Parameter: nameof(ApprovalSetting.Id)));

        private static void ValidateStorageApprovalSetting(ApprovalSetting maybeApprovalSetting, Guid approvalSettingId)
        {
            if (maybeApprovalSetting is null)
            {
                throw new NotFoundApprovalSettingException(
                    message: $"Approval setting not found with id: {approvalSettingId}.");
            }
        }

        private static void ValidateApprovalSettingIsNotNull(ApprovalSetting approvalSetting)
        {
            if (approvalSetting is null)
            {
                throw new NullApprovalSettingException(message: "Approval setting is null.");
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
            var invalidApprovalSettingException = new InvalidApprovalSettingException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalSettingException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalSettingException.ThrowIfContainsErrors();
        }
    }
}
