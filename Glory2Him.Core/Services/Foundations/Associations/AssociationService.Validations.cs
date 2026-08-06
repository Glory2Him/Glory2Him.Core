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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller
        //
        // Association has no scoped roles of its own (design §14.7, §18.6) — authorization
        // is derived from the two endpoint entity types instead. That derivation is an
        // orchestration-level concern (needs both endpoints resolved) and lands in a
        // follow-up change; until then this foundation enforces only the global roles.

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked = securityContext.Roles.Contains(Roles.ReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");
            }
        }

        // the moderation roles that may act on and read non-public versions for review and
        // audit (Reviewer, Publisher, Admin — global only until endpoint-derived
        // authorization lands, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower workflow rules stay in the orchestration, which needs owner writes for
        // resubmission and role writes for the publish flip
        private async ValueTask ValidateUserCanModifyStorageAssociationAsync(
            Association storageAssociation,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageAssociation.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to modify this content item association.");
            }
        }

        // removing an association is a takedown, not a moderation step — the owner may
        // remove their own association and an Admin may remove anyone's; Reviewers and
        // Publishers moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageAssociationAsync(
            Association storageAssociation,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageAssociation.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to remove this content item association.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveAssociation(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to permanently remove " +
                        "this content item association.");
            }
        }

        private async ValueTask ValidateOnAddAssociationAsync(
            Association association,
            SecurityContext securityContext)
        {
            ValidateAssociationIsNotNull(association);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(association.Id),
                    Parameter: nameof(Association.Id)),

                (Rule: IsInvalid(association.LinkedEntityId),
                    Parameter: nameof(Association.LinkedEntityId)),

                (Rule: IsInvalid(association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsInvalid(association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsInvalid(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)),

                (Rule: IsInvalid(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsGreaterThan(association.CreatedBy, 255),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsGreaterThan(association.UpdatedBy, 255),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsGreaterThan(association.AssociationConfidenceReason, 500),
                    Parameter: nameof(Association.AssociationConfidenceReason)),

                (Rule: IsNotWithinRange(association.AssociationConfidenceScore, 0, 10),
                    Parameter: nameof(Association.AssociationConfidenceScore)),

                (Rule: IsNotSame(
                        firstDate: association.UpdatedWhen,
                        secondDate: association.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsNotSame(
                        first: association.UpdatedBy,
                        second: association.CreatedBy,
                        secondName: nameof(Association.CreatedBy)),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: await IsNotRecentAsync(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyAssociationAsync(
            Association association,
            SecurityContext securityContext)
        {
            ValidateAssociationIsNotNull(association);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(association.Id),
                    Parameter: nameof(Association.Id)),

                (Rule: IsInvalid(association.LinkedEntityId),
                    Parameter: nameof(Association.LinkedEntityId)),

                (Rule: IsInvalid(association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsInvalid(association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsInvalid(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)),

                (Rule: IsInvalid(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsGreaterThan(association.CreatedBy, 255),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsGreaterThan(association.UpdatedBy, 255),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsGreaterThan(association.AssociationConfidenceReason, 500),
                    Parameter: nameof(Association.AssociationConfidenceReason)),

                (Rule: IsNotWithinRange(association.AssociationConfidenceScore, 0, 10),
                    Parameter: nameof(Association.AssociationConfidenceScore)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: association.UpdatedWhen,
                        secondDate: association.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)));
        }

        private static void ValidateAssociationEventEnvelope(
            EventEnvelope<Association> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        private static void ValidateAgainstStorageAssociationOnModify(
            Association inputAssociation,
            Association storageAssociation)
        {
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputAssociation.CreatedWhen,
                        secondDate: storageAssociation.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputAssociation.CreatedBy,
                        second: storageAssociation.CreatedBy,
                        secondName: nameof(Association.CreatedBy)),
                    Parameter: nameof(Association.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputAssociation.UpdatedWhen,
                        secondDate: storageAssociation.UpdatedWhen,
                        secondDateName: nameof(Association.UpdatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateOnRemoveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateOnHardRemoveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateStorageAssociation(
            Association maybeAssociation,
            Guid associationId)
        {
            if (maybeAssociation is null)
            {
                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }
        }

        private static void ValidateAssociationIsNotNull(
            Association association)
        {
            if (association is null)
            {
                throw new NullAssociationException(message: "Content item association is null.");
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

        // the score is optional — only a supplied value is range checked
        private static dynamic IsNotWithinRange(int? value, int minimum, int maximum) => new
        {
            Condition = value.HasValue && (value < minimum || value > maximum),
            Message = $"Value is not within range of {minimum} and {maximum}"
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
            var invalidAssociationException = new InvalidAssociationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidAssociationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidAssociationException.ThrowIfContainsErrors();
        }
    }
}
