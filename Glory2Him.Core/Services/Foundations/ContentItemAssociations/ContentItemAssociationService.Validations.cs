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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationService
    {
        private async ValueTask ValidateOnAddContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            SecurityContext securityContext)
        {
            ValidateContentItemAssociationIsNotNull(contentItemAssociation);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(contentItemAssociation.Id),
                    Parameter: nameof(ContentItemAssociation.Id)),

                (Rule: IsInvalid(contentItemAssociation.LinkedEntityId),
                    Parameter: nameof(ContentItemAssociation.LinkedEntityId)),

                (Rule: IsInvalid(contentItemAssociation.CreatedBy),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),

                (Rule: IsInvalid(contentItemAssociation.UpdatedBy),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: IsInvalid(contentItemAssociation.CreatedWhen),
                    Parameter: nameof(ContentItemAssociation.CreatedWhen)),

                (Rule: IsInvalid(contentItemAssociation.UpdatedWhen),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItemAssociation.CreatedBy, 255),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),

                (Rule: IsGreaterThan(contentItemAssociation.UpdatedBy, 255),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: contentItemAssociation.UpdatedWhen,
                        secondDate: contentItemAssociation.CreatedWhen,
                        secondDateName: nameof(ContentItemAssociation.CreatedWhen)),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItemAssociation.CreatedBy),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),

                (Rule: IsNotSame(
                        first: contentItemAssociation.UpdatedBy,
                        second: contentItemAssociation.CreatedBy,
                        secondName: nameof(ContentItemAssociation.CreatedBy)),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: await IsNotRecentAsync(contentItemAssociation.CreatedWhen),
                    Parameter: nameof(ContentItemAssociation.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyContentItemAssociationAsync(
            ContentItemAssociation contentItemAssociation,
            SecurityContext securityContext)
        {
            ValidateContentItemAssociationIsNotNull(contentItemAssociation);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(contentItemAssociation.Id),
                    Parameter: nameof(ContentItemAssociation.Id)),

                (Rule: IsInvalid(contentItemAssociation.LinkedEntityId),
                    Parameter: nameof(ContentItemAssociation.LinkedEntityId)),

                (Rule: IsInvalid(contentItemAssociation.CreatedBy),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),

                (Rule: IsInvalid(contentItemAssociation.UpdatedBy),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: IsInvalid(contentItemAssociation.CreatedWhen),
                    Parameter: nameof(ContentItemAssociation.CreatedWhen)),

                (Rule: IsInvalid(contentItemAssociation.UpdatedWhen),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItemAssociation.CreatedBy, 255),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),

                (Rule: IsGreaterThan(contentItemAssociation.UpdatedBy, 255),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItemAssociation.UpdatedBy),
                    Parameter: nameof(ContentItemAssociation.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: contentItemAssociation.UpdatedWhen,
                        secondDate: contentItemAssociation.CreatedWhen,
                        secondDateName: nameof(ContentItemAssociation.CreatedWhen)),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(contentItemAssociation.UpdatedWhen),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)));
        }

        private static void ValidateContentItemAssociationEventEnvelope(
            EventEnvelope<ContentItemAssociation> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageContentItemAssociationOnModify(
            ContentItemAssociation inputContentItemAssociation,
            ContentItemAssociation storageContentItemAssociation)
        {
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputContentItemAssociation.CreatedWhen,
                        secondDate: storageContentItemAssociation.CreatedWhen,
                        secondDateName: nameof(ContentItemAssociation.CreatedWhen)),
                    Parameter: nameof(ContentItemAssociation.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputContentItemAssociation.CreatedBy,
                        second: storageContentItemAssociation.CreatedBy,
                        secondName: nameof(ContentItemAssociation.CreatedBy)),
                    Parameter: nameof(ContentItemAssociation.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputContentItemAssociation.UpdatedWhen,
                        secondDate: storageContentItemAssociation.UpdatedWhen,
                        secondDateName: nameof(ContentItemAssociation.UpdatedWhen)),
                    Parameter: nameof(ContentItemAssociation.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveContentItemAssociationById(Guid contentItemAssociationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemAssociationId), Parameter: nameof(ContentItemAssociation.Id)));

        private static void ValidateOnRemoveContentItemAssociationById(Guid contentItemAssociationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemAssociationId), Parameter: nameof(ContentItemAssociation.Id)));

        private static void ValidateOnHardRemoveContentItemAssociationById(Guid contentItemAssociationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemAssociationId), Parameter: nameof(ContentItemAssociation.Id)));

        private static void ValidateStorageContentItemAssociation(
            ContentItemAssociation maybeContentItemAssociation,
            Guid contentItemAssociationId)
        {
            if (maybeContentItemAssociation is null)
            {
                throw new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {contentItemAssociationId}.");
            }
        }

        private static void ValidateContentItemAssociationIsNotNull(
            ContentItemAssociation contentItemAssociation)
        {
            if (contentItemAssociation is null)
            {
                throw new NullContentItemAssociationException(message: "Content item association is null.");
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
            var invalidContentItemAssociationException = new InvalidContentItemAssociationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemAssociationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemAssociationException.ThrowIfContainsErrors();
        }
    }
}
