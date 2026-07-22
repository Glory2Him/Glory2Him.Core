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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Links
{
    public partial class LinkService
    {
        private async ValueTask ValidateOnAddLinkAsync(
            Link link,
            SecurityContext securityContext)
        {
            ValidateLinkIsNotNull(link);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Id), Parameter: nameof(Link.Id)),
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)),
                (Rule: IsInvalid(link.ContentItemGroupId), Parameter: nameof(Link.ContentItemGroupId)),
                (Rule: IsInvalid(link.CreatedBy), Parameter: nameof(Link.CreatedBy)),
                (Rule: IsInvalid(link.UpdatedBy), Parameter: nameof(Link.UpdatedBy)),
                (Rule: IsInvalid(link.CreatedWhen), Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsInvalid(link.UpdatedWhen), Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsGreaterThan(link.Name, 255),
                    Parameter: nameof(Link.Name)),

                (Rule: IsGreaterThan(link.Url, 2048),
                    Parameter: nameof(Link.Url)),

                (Rule: IsGreaterThan(link.LinkType, 64),
                    Parameter: nameof(Link.LinkType)),

                (Rule: IsGreaterThan(link.CreatedBy, 255),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsGreaterThan(link.UpdatedBy, 255),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsNotSame(
                        firstDate: link.UpdatedWhen,
                        secondDate: link.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: link.CreatedBy),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsNotSame(
                        first: link.UpdatedBy,
                        second: link.CreatedBy,
                        secondName: nameof(Link.CreatedBy)),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: await IsNotRecentAsync(link.CreatedWhen),
                    Parameter: nameof(Link.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyLinkAsync(
            Link link,
            SecurityContext securityContext)
        {
            ValidateLinkIsNotNull(link);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Id), Parameter: nameof(Link.Id)),
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)),
                (Rule: IsInvalid(link.ContentItemGroupId), Parameter: nameof(Link.ContentItemGroupId)),
                (Rule: IsInvalid(link.CreatedBy), Parameter: nameof(Link.CreatedBy)),
                (Rule: IsInvalid(link.UpdatedBy), Parameter: nameof(Link.UpdatedBy)),
                (Rule: IsInvalid(link.CreatedWhen), Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsInvalid(link.UpdatedWhen), Parameter: nameof(Link.UpdatedWhen)),

                (Rule: IsGreaterThan(link.Name, 255),
                    Parameter: nameof(Link.Name)),

                (Rule: IsGreaterThan(link.Url, 2048),
                    Parameter: nameof(Link.Url)),

                (Rule: IsGreaterThan(link.LinkType, 64),
                    Parameter: nameof(Link.LinkType)),

                (Rule: IsGreaterThan(link.CreatedBy, 255),
                    Parameter: nameof(Link.CreatedBy)),

                (Rule: IsGreaterThan(link.UpdatedBy, 255),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: link.UpdatedBy),
                    Parameter: nameof(Link.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: link.UpdatedWhen,
                        secondDate: link.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(link.UpdatedWhen),
                    Parameter: nameof(Link.UpdatedWhen)));
        }

        private static void ValidateLinkEventEnvelope(EventEnvelope<Link> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidLinkEventException(
                    message: "Invalid link event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageLinkOnModify(
            Link inputLink,
            Link storageLink)
        {
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputLink.CreatedWhen,
                        secondDate: storageLink.CreatedWhen,
                        secondDateName: nameof(Link.CreatedWhen)),
                    Parameter: nameof(Link.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputLink.CreatedBy,
                        second: storageLink.CreatedBy,
                        secondName: nameof(Link.CreatedBy)),
                    Parameter: nameof(Link.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputLink.UpdatedWhen,
                        secondDate: storageLink.UpdatedWhen,
                        secondDateName: nameof(Link.UpdatedWhen)),
                    Parameter: nameof(Link.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveLinkById(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static void ValidateOnRemoveLinkById(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static void ValidateOnHardRemoveLinkById(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static void ValidateStorageLink(Link maybeLink, Guid linkId)
        {
            if (maybeLink is null)
            {
                throw new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");
            }
        }

        private static void ValidateLinkIsNotNull(Link link)
        {
            if (link is null)
            {
                throw new NullLinkException(message: "Link is null.");
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
            var invalidLinkException = new InvalidLinkException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidLinkException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidLinkException.ThrowIfContainsErrors();
        }
    }
}
