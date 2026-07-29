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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        private async ValueTask ValidateOnAddContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateContentItemIsNotNull(contentItem);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentTypeId), Parameter: nameof(ContentItem.ContentTypeId)),
                (Rule: IsInvalid(contentItem.ContentItemGroupId), Parameter: nameof(ContentItem.ContentItemGroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItem.CreatedBy, 255),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsGreaterThan(contentItem.UpdatedBy, 255),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: contentItem.UpdatedWhen,
                        secondDate: contentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItem.CreatedBy),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsNotSame(
                        first: contentItem.UpdatedBy,
                        second: contentItem.CreatedBy,
                        secondName: nameof(ContentItem.CreatedBy)),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: await IsNotRecentAsync(contentItem.CreatedWhen),
                    Parameter: nameof(ContentItem.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateContentItemIsNotNull(contentItem);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentTypeId), Parameter: nameof(ContentItem.ContentTypeId)),
                (Rule: IsInvalid(contentItem.ContentItemGroupId), Parameter: nameof(ContentItem.ContentItemGroupId)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)),
                (Rule: IsInvalid(contentItem.CreatedBy), Parameter: nameof(ContentItem.CreatedBy)),
                (Rule: IsInvalid(contentItem.UpdatedBy), Parameter: nameof(ContentItem.UpdatedBy)),
                (Rule: IsInvalid(contentItem.CreatedWhen), Parameter: nameof(ContentItem.CreatedWhen)),
                (Rule: IsInvalid(contentItem.UpdatedWhen), Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: IsGreaterThan(contentItem.CreatedBy, 255),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsGreaterThan(contentItem.UpdatedBy, 255),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: contentItem.UpdatedBy),
                    Parameter: nameof(ContentItem.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: contentItem.UpdatedWhen,
                        secondDate: contentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(contentItem.UpdatedWhen),
                    Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateOnRemoveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateOnHardRemoveContentItemById(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateStorageContentItem(ContentItem maybeContentItem, Guid contentItemId)
        {
            if (maybeContentItem is null)
            {
                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }
        }

        private static void ValidateAgainstStorageContentItemOnModify(
            ContentItem inputContentItem,
            ContentItem storageContentItem)
        {
            Validate(
                message: "Content item is invalid, fix the errors and try again.",

                (Rule: IsNotSame(
                        firstDate: inputContentItem.CreatedWhen,
                        secondDate: storageContentItem.CreatedWhen,
                        secondDateName: nameof(ContentItem.CreatedWhen)),
                    Parameter: nameof(ContentItem.CreatedWhen)),

                (Rule: IsNotSame(
                        first: inputContentItem.CreatedBy,
                        second: storageContentItem.CreatedBy,
                        secondName: nameof(ContentItem.CreatedBy)),
                    Parameter: nameof(ContentItem.CreatedBy)),

                (Rule: IsSame(
                        firstDate: inputContentItem.UpdatedWhen,
                        secondDate: storageContentItem.UpdatedWhen,
                        secondDateName: nameof(ContentItem.UpdatedWhen)),
                    Parameter: nameof(ContentItem.UpdatedWhen)));
        }

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemException(message: "Content item is null.");
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

        private static dynamic IsGreaterThan(string text, int maxLength) => new
        {
            Condition = IsExceedingLength(text, maxLength),
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private static bool IsExceedingLength(string text, int maxLength) =>
            (text ?? string.Empty).Length > maxLength;

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsNotSame(
            string first,
            string second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Text is not the same as {secondName}"
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
            var invalidContentItemException = new InvalidContentItemException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemException.ThrowIfContainsErrors();
        }
    }
}
