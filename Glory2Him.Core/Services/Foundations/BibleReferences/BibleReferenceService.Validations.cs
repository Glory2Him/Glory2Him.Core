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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    internal partial class BibleReferenceService
    {
        private async ValueTask ValidateOnAddBibleReferenceAsync(
            BibleReference bibleReference,
            SecurityContext securityContext)
        {
            ValidateBibleReferenceIsNotNull(bibleReference);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsInvalid(bibleReference.Id), Parameter: nameof(BibleReference.Id)),
                (Rule: IsInvalid(bibleReference.Reference), Parameter: nameof(BibleReference.Reference)),
                (Rule: IsInvalid(bibleReference.Translation), Parameter: nameof(BibleReference.Translation)),

                (Rule: IsInvalid(bibleReference.ContentItemGroupId),
                    Parameter: nameof(BibleReference.ContentItemGroupId)),

                (Rule: IsInvalid(bibleReference.CreatedBy), Parameter: nameof(BibleReference.CreatedBy)),
                (Rule: IsInvalid(bibleReference.UpdatedBy), Parameter: nameof(BibleReference.UpdatedBy)),
                (Rule: IsInvalid(bibleReference.CreatedWhen), Parameter: nameof(BibleReference.CreatedWhen)),
                (Rule: IsInvalid(bibleReference.UpdatedWhen), Parameter: nameof(BibleReference.UpdatedWhen)),

                (Rule: IsGreaterThan(bibleReference.Reference, 255),
                    Parameter: nameof(BibleReference.Reference)),

                (Rule: IsGreaterThan(bibleReference.Translation, 50),
                    Parameter: nameof(BibleReference.Translation)),

                (Rule: IsGreaterThan(bibleReference.CreatedBy, 255),
                    Parameter: nameof(BibleReference.CreatedBy)),

                (Rule: IsGreaterThan(bibleReference.UpdatedBy, 255),
                    Parameter: nameof(BibleReference.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: bibleReference.UpdatedWhen,
                        secondDate: bibleReference.CreatedWhen,
                        secondDateName: nameof(BibleReference.CreatedWhen)),
                    Parameter: nameof(BibleReference.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: bibleReference.CreatedBy),
                    Parameter: nameof(BibleReference.CreatedBy)),

                (Rule: IsNotSame(
                        first: bibleReference.UpdatedBy,
                        second: bibleReference.CreatedBy,
                        secondName: nameof(BibleReference.CreatedBy)),
                    Parameter: nameof(BibleReference.UpdatedBy)),

                (Rule: await IsNotRecentAsync(bibleReference.CreatedWhen),
                    Parameter: nameof(BibleReference.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyBibleReferenceAsync(
            BibleReference bibleReference,
            SecurityContext securityContext)
        {
            ValidateBibleReferenceIsNotNull(bibleReference);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsInvalid(bibleReference.Id), Parameter: nameof(BibleReference.Id)),
                (Rule: IsInvalid(bibleReference.Reference), Parameter: nameof(BibleReference.Reference)),
                (Rule: IsInvalid(bibleReference.Translation), Parameter: nameof(BibleReference.Translation)),

                (Rule: IsInvalid(bibleReference.ContentItemGroupId),
                    Parameter: nameof(BibleReference.ContentItemGroupId)),

                (Rule: IsInvalid(bibleReference.CreatedBy), Parameter: nameof(BibleReference.CreatedBy)),
                (Rule: IsInvalid(bibleReference.UpdatedBy), Parameter: nameof(BibleReference.UpdatedBy)),
                (Rule: IsInvalid(bibleReference.CreatedWhen), Parameter: nameof(BibleReference.CreatedWhen)),
                (Rule: IsInvalid(bibleReference.UpdatedWhen), Parameter: nameof(BibleReference.UpdatedWhen)),

                (Rule: IsGreaterThan(bibleReference.Reference, 255),
                    Parameter: nameof(BibleReference.Reference)),

                (Rule: IsGreaterThan(bibleReference.Translation, 50),
                    Parameter: nameof(BibleReference.Translation)),

                (Rule: IsGreaterThan(bibleReference.CreatedBy, 255),
                    Parameter: nameof(BibleReference.CreatedBy)),

                (Rule: IsGreaterThan(bibleReference.UpdatedBy, 255),
                    Parameter: nameof(BibleReference.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: bibleReference.UpdatedBy),
                    Parameter: nameof(BibleReference.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: bibleReference.UpdatedWhen,
                        secondDate: bibleReference.CreatedWhen,
                        secondDateName: nameof(BibleReference.CreatedWhen)),
                    Parameter: nameof(BibleReference.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(bibleReference.UpdatedWhen),
                    Parameter: nameof(BibleReference.UpdatedWhen)));
        }

        private static void ValidateBibleReferenceEventEnvelope(EventEnvelope<BibleReference> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidBibleReferenceEventException(
                    message: "Invalid bible reference event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageBibleReferenceOnModify(
            BibleReference inputBibleReference,
            BibleReference storageBibleReference)
        {
            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputBibleReference.CreatedWhen,
                        secondDate: storageBibleReference.CreatedWhen,
                        secondDateName: nameof(BibleReference.CreatedWhen)),
                    Parameter: nameof(BibleReference.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputBibleReference.CreatedBy,
                        second: storageBibleReference.CreatedBy,
                        secondName: nameof(BibleReference.CreatedBy)),
                    Parameter: nameof(BibleReference.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputBibleReference.UpdatedWhen,
                        secondDate: storageBibleReference.UpdatedWhen,
                        secondDateName: nameof(BibleReference.UpdatedWhen)),
                    Parameter: nameof(BibleReference.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveBibleReferenceById(Guid bibleReferenceId) =>
            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsInvalid(bibleReferenceId), Parameter: nameof(BibleReference.Id)));

        private static void ValidateOnRemoveBibleReferenceById(Guid bibleReferenceId) =>
            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsInvalid(bibleReferenceId), Parameter: nameof(BibleReference.Id)));

        private static void ValidateOnHardRemoveBibleReferenceById(Guid bibleReferenceId) =>
            Validate(
                message: "Bible reference is invalid, fix the errors and try again.",
                (Rule: IsInvalid(bibleReferenceId), Parameter: nameof(BibleReference.Id)));

        private static void ValidateStorageBibleReference(BibleReference maybeBibleReference, Guid bibleReferenceId)
        {
            if (maybeBibleReference is null)
            {
                throw new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {bibleReferenceId}.");
            }
        }

        private static void ValidateBibleReferenceIsNotNull(BibleReference bibleReference)
        {
            if (bibleReference is null)
            {
                throw new NullBibleReferenceException(message: "Bible reference is null.");
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
            var invalidBibleReferenceException = new InvalidBibleReferenceException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidBibleReferenceException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidBibleReferenceException.ThrowIfContainsErrors();
        }
    }
}
