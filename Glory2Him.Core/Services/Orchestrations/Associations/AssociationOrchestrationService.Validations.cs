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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // The orchestration enforces the contribution gate itself (§SEC14.6): an exposer may bind
        // to it directly, so it never assumes an upstream layer already gated the caller.
        //
        // The ADD's composition: the two row-free leaves, and then — once both endpoints have
        // been resolved from storage — the endpoint half of the veto, which this member is the
        // one write able to decide for itself (§SEC14.7 posture A′ rule 4, "the add is the
        // exception that proves the rule").
        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            ValidateUserIsAuthenticated(securityContext);
            ValidateUserIsNotGloballyBlocked(securityContext);
        }

        // The orchestration's half of the gate on modify, remove and hard remove: the part that
        // needs NO row (§SEC14.7 posture A′ rule 4). Each of the three is handed an id or an
        // untrusted Association, so nothing composed from the STORED endpoints is decidable here
        // — that half runs in the foundation, and this layer issues no second read to duplicate
        // it. Running these two first is what stops the three surfaces being used to probe which
        // association ids exist.
        //
        // Deliberately NOT the same method as the add's gate above, though the two compose the
        // same leaves today. They compose them for different reasons, and a rule added to one
        // must not silently bind the other.
        private static void ValidateUserMayWriteWithoutTheStoredRow(SecurityContext securityContext)
        {
            ValidateUserIsAuthenticated(securityContext);
            ValidateUserIsNotGloballyBlocked(securityContext);
        }

        // HARD REMOVE's composition: the same two row-free leaves, plus Administrators — which is
        // itself decidable with no row, so it joins this layer's half rather than the
        // foundation's (§SEC14.7 posture A′ rule 4). The endpoint veto is NOT here; it needs the
        // stored row and stays one layer down, where a block refuses even an administrator
        // (§SEC18.6 rule 2).
        //
        // The global block is asked BEFORE the Administrators grant, because a veto is asked
        // ahead of any grant and is overridden by none of them.
        private static void ValidateUserMayHardRemoveAssociation(SecurityContext securityContext)
        {
            ValidateUserIsAuthenticated(securityContext);
            ValidateUserIsNotGloballyBlocked(securityContext);

            if (securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not permitted to permanently delete a content item association.");
            }
        }

        private static void ValidateUserIsAuthenticated(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");
            }
        }

        private static void ValidateUserIsNotGloballyBlocked(SecurityContext securityContext)
        {
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");
            }
        }

        // §SEC14.7 posture A′ rule 1's veto, composed from the endpoints this service has just
        // RESOLVED from storage — never from the caller's copy, which is why the content type may
        // be trusted here at all. Four scoped names in all, two per end.
        //
        // THE `OR` IS LOAD-BEARING. Under an AND, a user holding Tag-ReadOnly alongside
        // BibleReference-Reviewers could pair a tag with an entity type they are not banned from
        // and land it on a public scripture page — exactly what Tag-ReadOnly exists to prevent.
        // One end admits on the grant side; one end bars on the block side.
        //
        // THIS IS THE ADD'S ALONE. Modify, remove and hard remove are handed an id or an
        // untrusted row, so there is no resolved endpoint for this layer to compose from and the
        // veto belongs one layer down (§SEC14.7 posture A′ rule 4). §SEC14.6 rule 2 makes the
        // duplicate with the foundation's own gate intended rather than redundant.
        private static void ValidateUserIsNotBlockedFromEndpoints(
            SecurityContext securityContext,
            Association association)
        {
            bool isBlocked =
                IsBlockedFromEndpoint(
                    securityContext,
                    association.EntityAType,
                    association.EntityAContentType)
                || IsBlockedFromEndpoint(
                    securityContext,
                    association.EntityBType,
                    association.EntityBContentType);

            if (isBlocked)
            {
                throw new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");
            }
        }

        // Both block tiers for ONE endpoint. A null content type costs that endpoint its narrow
        // tier and widens nothing: only ContentItem carries one (§SEC18.6 rule 5), and on this
        // path a ContentItem endpoint always has one, because resolution derived it.
        private static bool IsBlockedFromEndpoint(
            SecurityContext securityContext,
            EntityType entityType,
            ContentType? contentType)
        {
            if (securityContext.Roles.Contains(Roles.ReadOnlyFor(entityType)))
            {
                return true;
            }

            return contentType.HasValue
                && securityContext.Roles.Contains(
                    Roles.ReadOnlyFor(entityType, contentType.Value));
        }

        private static void ValidateAssociationIsNotNull(Association association)
        {
            if (association is null)
            {
                throw new NullAssociationOrchestrationException(
                    message: "Content item association is null.");
            }
        }

        // Only the RAW endpoints are the caller's to supply: each side's entity type and key id.
        // Everything else about an endpoint — scope, group id, content type — is derived by
        // resolution and must not be validated (or trusted) here.
        private static void ValidateOnAddAssociation(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.EntityAType), Parameter: nameof(Association.EntityAType)),
                (Rule: IsInvalid(association.EntityBType), Parameter: nameof(Association.EntityBType)),
                (Rule: IsInvalid(association.EntityAKeyId), Parameter: nameof(Association.EntityAKeyId)),
                (Rule: IsInvalid(association.EntityBKeyId), Parameter: nameof(Association.EntityBKeyId)));

        // The id-keyed surfaces' own validation. Kept separate from ValidateOnAddAssociation
        // rather than folded into a shared validator: they compose different rules today and
        // sharing the composition would mean a rule added for one silently binds the other.
        private static void ValidateAssociationId(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(EntityType entityType) => new
        {
            Condition = Enum.IsDefined(entityType) is false,
            Message = "Value is not a recognized entity type"
        };

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidAssociationOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidAssociationOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
