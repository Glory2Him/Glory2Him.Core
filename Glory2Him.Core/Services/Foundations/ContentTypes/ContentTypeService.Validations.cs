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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    internal partial class ContentTypeService
    {
        private async ValueTask ValidateOnAddContentTypeAsync(
            ContentType contentType,
            SecurityContext securityContext)
        {
            ValidateContentTypeIsNotNull(contentType);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentType.Id), Parameter: nameof(ContentType.Id)),
                (Rule: IsInvalid(contentType.Name), Parameter: nameof(ContentType.Name)),
                (Rule: IsInvalid(contentType.CreatedBy), Parameter: nameof(ContentType.CreatedBy)),
                (Rule: IsInvalid(contentType.UpdatedBy), Parameter: nameof(ContentType.UpdatedBy)),
                (Rule: IsInvalid(contentType.CreatedWhen), Parameter: nameof(ContentType.CreatedWhen)),
                (Rule: IsInvalid(contentType.UpdatedWhen), Parameter: nameof(ContentType.UpdatedWhen)),

                (Rule: IsGreaterThan(contentType.Name, 255),
                    Parameter: nameof(ContentType.Name)),

                (Rule: IsGreaterThan(contentType.CreatedBy, 255),
                    Parameter: nameof(ContentType.CreatedBy)),

                (Rule: IsGreaterThan(contentType.UpdatedBy, 255),
                    Parameter: nameof(ContentType.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: contentType.UpdatedWhen,
                        secondDate: contentType.CreatedWhen,
                        secondDateName: nameof(ContentType.CreatedWhen)),
                    Parameter: nameof(ContentType.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentType.CreatedBy),
                    Parameter: nameof(ContentType.CreatedBy)),

                (Rule: IsNotSame(
                        first: contentType.UpdatedBy,
                        second: contentType.CreatedBy,
                        secondName: nameof(ContentType.CreatedBy)),
                    Parameter: nameof(ContentType.UpdatedBy)),

                (Rule: await IsNotRecentAsync(contentType.CreatedWhen),
                    Parameter: nameof(ContentType.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyContentTypeAsync(
            ContentType contentType,
            SecurityContext securityContext)
        {
            ValidateContentTypeIsNotNull(contentType);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentType.Id), Parameter: nameof(ContentType.Id)),
                (Rule: IsInvalid(contentType.Name), Parameter: nameof(ContentType.Name)),
                (Rule: IsInvalid(contentType.CreatedBy), Parameter: nameof(ContentType.CreatedBy)),
                (Rule: IsInvalid(contentType.UpdatedBy), Parameter: nameof(ContentType.UpdatedBy)),
                (Rule: IsInvalid(contentType.CreatedWhen), Parameter: nameof(ContentType.CreatedWhen)),
                (Rule: IsInvalid(contentType.UpdatedWhen), Parameter: nameof(ContentType.UpdatedWhen)),

                (Rule: IsGreaterThan(contentType.Name, 255),
                    Parameter: nameof(ContentType.Name)),

                (Rule: IsGreaterThan(contentType.CreatedBy, 255),
                    Parameter: nameof(ContentType.CreatedBy)),

                (Rule: IsGreaterThan(contentType.UpdatedBy, 255),
                    Parameter: nameof(ContentType.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentType.UpdatedBy),
                    Parameter: nameof(ContentType.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: contentType.UpdatedWhen,
                        secondDate: contentType.CreatedWhen,
                        secondDateName: nameof(ContentType.CreatedWhen)),
                    Parameter: nameof(ContentType.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(contentType.UpdatedWhen),
                    Parameter: nameof(ContentType.UpdatedWhen)));
        }

        private static void ValidateContentTypeEventEnvelope(EventEnvelope<ContentType> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentTypeEventException(
                    message: "Invalid content type event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageContentTypeOnModify(
            ContentType inputContentType,
            ContentType storageContentType)
        {
            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputContentType.CreatedWhen,
                        secondDate: storageContentType.CreatedWhen,
                        secondDateName: nameof(ContentType.CreatedWhen)),
                    Parameter: nameof(ContentType.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputContentType.CreatedBy,
                        second: storageContentType.CreatedBy,
                        secondName: nameof(ContentType.CreatedBy)),
                    Parameter: nameof(ContentType.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputContentType.UpdatedWhen,
                        secondDate: storageContentType.UpdatedWhen,
                        secondDateName: nameof(ContentType.UpdatedWhen)),
                    Parameter: nameof(ContentType.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveContentTypeById(Guid contentTypeId) =>
            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentTypeId), Parameter: nameof(ContentType.Id)));

        private static void ValidateOnRemoveContentTypeById(Guid contentTypeId) =>
            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentTypeId), Parameter: nameof(ContentType.Id)));

        private static void ValidateOnHardRemoveContentTypeById(Guid contentTypeId) =>
            Validate(
                message: "Content type is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentTypeId), Parameter: nameof(ContentType.Id)));

        private static void ValidateStorageContentType(ContentType maybeContentType, Guid contentTypeId)
        {
            if (maybeContentType is null)
            {
                throw new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");
            }
        }

        private static void ValidateContentTypeIsNotNull(ContentType contentType)
        {
            if (contentType is null)
            {
                throw new NullContentTypeException(message: "Content type is null.");
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
            var invalidContentTypeException = new InvalidContentTypeException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentTypeException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentTypeException.ThrowIfContainsErrors();
        }
    }
}
