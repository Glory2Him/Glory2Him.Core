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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
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

                (Rule: IsInvalid(association.EntityAType),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsInvalid(association.EntityAKeyId),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsInvalid(association.EntityAGroupId),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsInvalid(association.EntityBType),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsInvalid(association.EntityBKeyId),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsInvalid(association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsSameEndpoint(
                        association.EntityAGroupId,
                        association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityAType,
                        association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsInvalid(association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityBType,
                        association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

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

                (Rule: IsGreaterThan(association.UserId, 255),
                    Parameter: nameof(Association.UserId)),

                (Rule: IsGreaterThan(association.ConfidenceReason, 500),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsGreaterThan(association.ModelVersion, 128),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsNotWithinRange(association.ConfidenceScore, 0, 10),
                    Parameter: nameof(Association.ConfidenceScore)),

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

                (Rule: IsInvalid(association.EntityAType),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsInvalid(association.EntityAKeyId),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsInvalid(association.EntityAGroupId),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsInvalid(association.EntityBType),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsInvalid(association.EntityBKeyId),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsInvalid(association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsSameEndpoint(
                        association.EntityAGroupId,
                        association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityAType,
                        association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsInvalid(association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityBType,
                        association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

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

                (Rule: IsGreaterThan(association.UserId, 255),
                    Parameter: nameof(Association.UserId)),

                (Rule: IsGreaterThan(association.ConfidenceReason, 500),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsGreaterThan(association.ModelVersion, 128),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsNotWithinRange(association.ConfidenceScore, 0, 10),
                    Parameter: nameof(Association.ConfidenceScore)),

                // scope is the one endpoint field a modify may move, so unlike add it is
                // validated rather than derived — re-deriving here would overwrite a
                // legitimate narrowing and pre-empt the set-scope operation (design §9.7.1
                // rule 6). The endpoint type is pinned against storage, so checking the
                // scope against the input's own type is sound.
                (Rule: IsInvalid(association.EntityAScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsScopeNotApplicable(
                        association.EntityAType,
                        association.EntityAScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsInvalid(association.EntityBScope),
                    Parameter: nameof(Association.EntityBScope)),

                (Rule: IsScopeNotApplicable(
                        association.EntityBType,
                        association.EntityBScope),
                    Parameter: nameof(Association.EntityBScope)),

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

        // reclassification is forbidden: an association is a link between two specific
        // entities, and repointing it is indistinguishable from deleting one link and
        // creating another — except that it carries the original's approval state and
        // review history across to a pair nobody reviewed. Type, KeyId and GroupId are
        // therefore pinned against storage on both endpoints; Scope is the one endpoint
        // field that may change, and only through the set-scope operation (design §9.7.1
        // rule 6).
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

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAType,
                        second: storageAssociation.EntityAType,
                        secondName: nameof(Association.EntityAType)),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAKeyId,
                        second: storageAssociation.EntityAKeyId,
                        secondName: nameof(Association.EntityAKeyId)),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAGroupId,
                        second: storageAssociation.EntityAGroupId,
                        secondName: nameof(Association.EntityAGroupId)),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBType,
                        second: storageAssociation.EntityBType,
                        secondName: nameof(Association.EntityBType)),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBKeyId,
                        second: storageAssociation.EntityBKeyId,
                        secondName: nameof(Association.EntityBKeyId)),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBGroupId,
                        second: storageAssociation.EntityBGroupId,
                        secondName: nameof(Association.EntityBGroupId)),
                    Parameter: nameof(Association.EntityBGroupId)),

                // the content type is pinned alongside the identity fields, not left with
                // scope. It is an authorization input (design §18.6) derived from the
                // resolved endpoint, and the endpoint cannot change — so neither can it.
                // Leaving it writable would let an approved association be re-labelled into
                // a content type whose reviewers never saw it.
                (Rule: IsNotSame(
                        first: inputAssociation.EntityAContentType,
                        second: storageAssociation.EntityAContentType,
                        secondName: nameof(Association.EntityAContentType)),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBContentType,
                        second: storageAssociation.EntityBContentType,
                        secondName: nameof(Association.EntityBContentType)),
                    Parameter: nameof(Association.EntityBContentType)),

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

        // structural validation for an enum crossing a boundary — rejects an out-of-range
        // value (a stale client sending a since-removed member); it cannot detect "caller
        // forgot to set it", since EntityType has no unset sentinel
        private static dynamic IsInvalid(EntityType entityType) => new
        {
            Condition = Enum.IsDefined(entityType) == false,
            Message = "Value is not a supported entity type"
        };

        private static dynamic IsInvalid(Scope scope) => new
        {
            Condition = Enum.IsDefined(scope) == false,
            Message = "Value is not a supported scope"
        };

        // only a supplied content type is range checked — null is the ordinary state for
        // every endpoint that is not a ContentItem, and for a ContentItem whose type the
        // orchestration has not resolved yet
        private static dynamic IsInvalid(ContentType? contentType) => new
        {
            Condition = contentType.HasValue && Enum.IsDefined(contentType.Value) == false,
            Message = "Value is not a supported content type"
        };

        // a non-versioned entity type has exactly one row, so AllVersions cannot mean
        // anything for it. On add this is unreachable because the scope is derived; on
        // modify the caller supplies it, and the derived invariant has to be defended
        // rather than silently re-imposed (design §9.7.1 rule 6).
        private static dynamic IsScopeNotApplicable(
            EntityType entityType,
            Scope scope) => new
            {
                Condition = Enum.IsDefined(entityType)
                    && EntityTypeVersioning.IsVersioned(entityType) is false
                    && scope == Scope.AllVersions,

                Message = "Value is only applicable to a versioned endpoint"
            };

        // one rule covers three mistakes at once: associating an entity with itself, with
        // another version of itself, and — since a non-versioned endpoint's group id is its
        // key id — a tag with itself
        private static dynamic IsSameEndpoint(
            Guid firstGroupId,
            Guid secondGroupId) => new
            {
                Condition = firstGroupId != Guid.Empty && firstGroupId == secondGroupId,
                Message = $"Value is the same as {nameof(Association.EntityAGroupId)}"
            };

        // only a ContentItem endpoint has a content type (design §18.6): no other entity
        // type has a sub-classification, so a value here on any other type is a caller
        // fabricating an authorization input. A ContentItem endpoint may still be null —
        // resolving it needs the endpoint row, which is the orchestration's read to make,
        // and a null simply costs the caller the narrow role tier.
        private static dynamic IsContentTypeNotApplicable(
            EntityType entityType,
            ContentType? contentType) => new
            {
                Condition = contentType is not null && entityType != EntityType.ContentItem,
                Message = $"Value is only applicable to a {nameof(EntityType.ContentItem)} endpoint"
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

        private static dynamic IsNotSame(
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            EntityType first,
            EntityType second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            ContentType? first,
            ContentType? second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        // the score is optional — only a supplied value is range checked. Null means not yet
        // scored, which is not the same as a zero and must not be treated as one.
        private static dynamic IsNotWithinRange(decimal? value, decimal minimum, decimal maximum) => new
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
