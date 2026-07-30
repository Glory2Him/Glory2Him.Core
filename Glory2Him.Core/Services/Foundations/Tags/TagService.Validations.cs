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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial class TagService
    {
        private async ValueTask ValidateOnAddTagAsync(
            Tag tag,
            SecurityContext securityContext)
        {
            ValidateTagIsNotNull(tag);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tag.Id), Parameter: nameof(Tag.Id)),
                (Rule: IsInvalid(tag.Name), Parameter: nameof(Tag.Name)),
                (Rule: IsInvalid(tag.CreatedBy), Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsInvalid(tag.UpdatedBy), Parameter: nameof(Tag.UpdatedBy)),
                (Rule: IsInvalid(tag.CreatedWhen), Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsInvalid(tag.UpdatedWhen), Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsGreaterThan(tag.Name, 30),
                    Parameter: nameof(Tag.Name)),

                (Rule: IsGreaterThan(tag.CreatedBy, 255),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsGreaterThan(tag.UpdatedBy, 255),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: tag.UpdatedWhen,
                        secondDate: tag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: tag.CreatedBy),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsNotSame(
                        first: tag.UpdatedBy,
                        second: tag.CreatedBy,
                        secondName: nameof(Tag.CreatedBy)),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: await IsNotRecentAsync(tag.CreatedWhen),
                    Parameter: nameof(Tag.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyTagAsync(
            Tag tag,
            SecurityContext securityContext)
        {
            ValidateTagIsNotNull(tag);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tag.Id), Parameter: nameof(Tag.Id)),
                (Rule: IsInvalid(tag.Name), Parameter: nameof(Tag.Name)),
                (Rule: IsInvalid(tag.CreatedBy), Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsInvalid(tag.UpdatedBy), Parameter: nameof(Tag.UpdatedBy)),
                (Rule: IsInvalid(tag.CreatedWhen), Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsInvalid(tag.UpdatedWhen), Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: IsGreaterThan(tag.Name, 30),
                    Parameter: nameof(Tag.Name)),

                (Rule: IsGreaterThan(tag.CreatedBy, 255),
                    Parameter: nameof(Tag.CreatedBy)),

                (Rule: IsGreaterThan(tag.UpdatedBy, 255),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: tag.UpdatedBy),
                    Parameter: nameof(Tag.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: tag.UpdatedWhen,
                        secondDate: tag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(tag.UpdatedWhen),
                    Parameter: nameof(Tag.UpdatedWhen)));
        }

        private static void ValidateTagEventEnvelope(EventEnvelope<Tag> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidTagEventException(
                    message: "Invalid tag event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageTagOnModify(
            Tag inputTag,
            Tag storageTag)
        {
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputTag.CreatedWhen,
                        secondDate: storageTag.CreatedWhen,
                        secondDateName: nameof(Tag.CreatedWhen)),
                    Parameter: nameof(Tag.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputTag.CreatedBy,
                        second: storageTag.CreatedBy,
                        secondName: nameof(Tag.CreatedBy)),
                    Parameter: nameof(Tag.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputTag.UpdatedWhen,
                        secondDate: storageTag.UpdatedWhen,
                        secondDateName: nameof(Tag.UpdatedWhen)),
                    Parameter: nameof(Tag.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveTagById(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        private static void ValidateOnRemoveTagById(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        private static void ValidateOnHardRemoveTagById(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        private static void ValidateStorageTag(Tag maybeTag, Guid tagId)
        {
            if (maybeTag is null)
            {
                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
            }
        }

        private static void ValidateTagIsNotNull(Tag tag)
        {
            if (tag is null)
            {
                throw new NullTagException(message: "Tag is null.");
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
            var invalidTagException = new InvalidTagException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidTagException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidTagException.ThrowIfContainsErrors();
        }
    }
}
