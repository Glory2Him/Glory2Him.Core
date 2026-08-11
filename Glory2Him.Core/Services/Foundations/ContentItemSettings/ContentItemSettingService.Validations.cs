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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ContentItemSettings
{
    internal partial class ContentItemSettingService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        // content item settings are administrator-authored display configuration — a
        // single Admin gate covers Add, Modify and Remove
        private static void ValidateUserIsAllowedToAdministerContentItemSettings(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemSettingException(
                    message: "The current user is not authenticated.");
            }

            if (HasAdminRole(securityContext) is false)
            {
                throw new UnauthorizedContentItemSettingException(
                    message: "The current user is not allowed to administer content item settings.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only, same as
        // every other content item setting write
        private static void ValidateUserCanHardRemoveContentItemSetting(SecurityContext securityContext) =>
            ValidateUserIsAllowedToAdministerContentItemSettings(securityContext);

        // the only role that may write settings — there is no read counterpart: settings
        // drive anonymous page rendering, so every non-deleted row is public (§14.1)
        private static bool HasAdminRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Admin);

        private async ValueTask ValidateOnAddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            SecurityContext securityContext)
        {
            ValidateContentItemSettingIsNotNull(contentItemSetting);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemSetting.Id), Parameter: nameof(ContentItemSetting.Id)),
                (Rule: IsInvalid(contentItemSetting.ContentType),
                    Parameter: nameof(ContentItemSetting.ContentType)),
                (Rule: IsInvalid(contentItemSetting.CreatedBy), Parameter: nameof(ContentItemSetting.CreatedBy)),
                (Rule: IsInvalid(contentItemSetting.UpdatedBy), Parameter: nameof(ContentItemSetting.UpdatedBy)),
                (Rule: IsInvalid(contentItemSetting.CreatedWhen), Parameter: nameof(ContentItemSetting.CreatedWhen)),
                (Rule: IsInvalid(contentItemSetting.UpdatedWhen), Parameter: nameof(ContentItemSetting.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItemSetting.CreatedBy, 255),
                    Parameter: nameof(ContentItemSetting.CreatedBy)),

                (Rule: IsGreaterThan(contentItemSetting.UpdatedBy, 255),
                    Parameter: nameof(ContentItemSetting.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: contentItemSetting.UpdatedWhen,
                        secondDate: contentItemSetting.CreatedWhen,
                        secondDateName: nameof(ContentItemSetting.CreatedWhen)),
                    Parameter: nameof(ContentItemSetting.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItemSetting.CreatedBy),
                    Parameter: nameof(ContentItemSetting.CreatedBy)),

                (Rule: IsNotSame(
                        first: contentItemSetting.UpdatedBy,
                        second: contentItemSetting.CreatedBy,
                        secondName: nameof(ContentItemSetting.CreatedBy)),
                    Parameter: nameof(ContentItemSetting.UpdatedBy)),

                (Rule: await IsNotRecentAsync(contentItemSetting.CreatedWhen),
                    Parameter: nameof(ContentItemSetting.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            SecurityContext securityContext)
        {
            ValidateContentItemSettingIsNotNull(contentItemSetting);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemSetting.Id), Parameter: nameof(ContentItemSetting.Id)),
                (Rule: IsInvalid(contentItemSetting.ContentType),
                    Parameter: nameof(ContentItemSetting.ContentType)),
                (Rule: IsInvalid(contentItemSetting.CreatedBy), Parameter: nameof(ContentItemSetting.CreatedBy)),
                (Rule: IsInvalid(contentItemSetting.UpdatedBy), Parameter: nameof(ContentItemSetting.UpdatedBy)),
                (Rule: IsInvalid(contentItemSetting.CreatedWhen), Parameter: nameof(ContentItemSetting.CreatedWhen)),
                (Rule: IsInvalid(contentItemSetting.UpdatedWhen), Parameter: nameof(ContentItemSetting.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItemSetting.CreatedBy, 255),
                    Parameter: nameof(ContentItemSetting.CreatedBy)),

                (Rule: IsGreaterThan(contentItemSetting.UpdatedBy, 255),
                    Parameter: nameof(ContentItemSetting.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItemSetting.UpdatedBy),
                    Parameter: nameof(ContentItemSetting.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: contentItemSetting.UpdatedWhen,
                        secondDate: contentItemSetting.CreatedWhen,
                        secondDateName: nameof(ContentItemSetting.CreatedWhen)),
                    Parameter: nameof(ContentItemSetting.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(contentItemSetting.UpdatedWhen),
                    Parameter: nameof(ContentItemSetting.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateContentItemSettingEventEnvelopeAsync(
            EventEnvelope<ContentItemSetting> envelope,
            ContentItemSettingEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(ContentItemSetting)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidContentItemSettingEventException(
                    message: "Invalid content item setting event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageContentItemSettingOnModify(
            ContentItemSetting inputContentItemSetting,
            ContentItemSetting storageContentItemSetting)
        {
            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputContentItemSetting.CreatedWhen,
                        secondDate: storageContentItemSetting.CreatedWhen,
                        secondDateName: nameof(ContentItemSetting.CreatedWhen)),
                    Parameter: nameof(ContentItemSetting.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputContentItemSetting.CreatedBy,
                        second: storageContentItemSetting.CreatedBy,
                        secondName: nameof(ContentItemSetting.CreatedBy)),
                    Parameter: nameof(ContentItemSetting.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputContentItemSetting.UpdatedWhen,
                        secondDate: storageContentItemSetting.UpdatedWhen,
                        secondDateName: nameof(ContentItemSetting.UpdatedWhen)),
                    Parameter: nameof(ContentItemSetting.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveContentItemSettingById(Guid contentItemSettingId) =>
            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemSettingId), Parameter: nameof(ContentItemSetting.Id)));

        private static void ValidateOnRemoveContentItemSettingById(Guid contentItemSettingId) =>
            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemSettingId), Parameter: nameof(ContentItemSetting.Id)));

        private static void ValidateOnHardRemoveContentItemSettingById(Guid contentItemSettingId) =>
            Validate(
                message: "Content item setting is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemSettingId), Parameter: nameof(ContentItemSetting.Id)));

        private static void ValidateStorageContentItemSetting(
            ContentItemSetting maybeContentItemSetting,
            Guid contentItemSettingId)
        {
            if (maybeContentItemSetting is null)
            {
                throw new NotFoundContentItemSettingException(
                    message: $"Content item setting not found with id: {contentItemSettingId}.");
            }
        }

        private static void ValidateContentItemSettingIsNotNull(ContentItemSetting contentItemSetting)
        {
            if (contentItemSetting is null)
            {
                throw new NullContentItemSettingException(message: "Content item setting is null.");
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

        private static dynamic IsInvalid(ContentType contentType) => new
        {
            Condition = Enum.IsDefined(contentType) == false,
            Message = "Value is not a supported content type"
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
            var invalidContentItemSettingException = new InvalidContentItemSettingException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemSettingException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemSettingException.ThrowIfContainsErrors();
        }
    }
}
