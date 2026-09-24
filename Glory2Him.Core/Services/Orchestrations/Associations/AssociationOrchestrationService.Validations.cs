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
using Glory2Him.Core.Models.Events.Foundations;
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

        // ── ONE COMPOSITION PER OPERATION, over shared leaves ─────────────────────────
        //
        // The three below are the orchestration's half of the gate on modify, remove and hard
        // remove: the part that needs NO row (§SEC14.7 posture A′ rule 4). Each of the three
        // members is handed an id or an untrusted Association, so nothing composed from the
        // STORED endpoints is decidable here — that half runs in the foundation, and this layer
        // issues no second read to duplicate it. Running these leaves first is what stops the
        // three surfaces being used to probe which association ids exist.
        //
        // MODIFY AND REMOVE COMPOSE THE SAME TWO LEAVES TODAY AND STILL GET A METHOD EACH, for
        // the reason the add's gate above gets its own: a shared composition cannot give one
        // operation a rule without giving it to the other, so the day modify needs something
        // remove must not have, the sharing is what makes the asymmetry unexpressible. The
        // duplication is three lines; the entanglement would be a rule arriving somewhere nobody
        // was looking. The leaves are shared freely — they carry no operation's policy.
        private static void ValidateUserMayModifyAssociation(SecurityContext securityContext)
        {
            ValidateUserIsAuthenticated(securityContext);
            ValidateUserIsNotGloballyBlocked(securityContext);
        }

        private static void ValidateUserMayRemoveAssociation(SecurityContext securityContext)
        {
            ValidateUserIsAuthenticated(securityContext);
            ValidateUserIsNotGloballyBlocked(securityContext);
        }

        // HARD REMOVE's composition: the same two row-free leaves, plus Administrators — which is
        // itself decidable with no row, so it joins this layer's half rather than the
        // foundation's (§SEC14.7 posture A′ rule 4). The endpoint veto is NOT here; it needs the
        // stored row and stays one layer down, where a block refuses even an administrator
        // (§SEC18.6 rule 2). This one is visibly not its neighbours' equal, which is the case the
        // rule above exists for.
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

        // THE EVENT PATH'S OWN GUARD, asked before this service reads anything. The foundation
        // asks the identical question again when the envelope is handed down, and that repeat is
        // §SEC14.6 rule 2 working as intended: this handler resolves BOTH endpoint rows before the
        // foundation is reached, and doing that on the word of an envelope nothing has vouched for
        // would be acting on an unattested payload (§SEC14.6 rule 4).
        //
        // The name is the publisher's composition — entity name plus operation — and it sits
        // inside the HMAC. It reads "AssociationAdding" because that is what EventBroker signs for
        // this address, exactly as the foundation composed it while the address bound there.
        private async ValueTask ValidateAssociationEventEnvelopeAsync(
            EventEnvelope<Association> envelope,
            AssociationEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidAssociationOrchestrationException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Association)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidAssociationOrchestrationException(
                    message: "Invalid content item association event. " +
                        "Integrity verification failed.");
            }
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

        // THE DERIVATION, EXPRESSED AS A REFUSAL — the event path's arm, and the difference from
        // the method path is the signature, not the rule. Both paths run the same write flow and
        // let the derived value govern. On the method path the derived value simply overwrites
        // the caller's: a loose object nobody attested to. Here the claim arrived inside a signed
        // envelope whose HMAC covers the content (§SEC14.6 rule 4), and the property that
        // signature buys is that no receiver edits a part the rules read — the foundation, the
        // ProcessedEvents record and the reply are all built from that content. So a claim that
        // disagrees with what the endpoints resolve to is refused rather than quietly corrected.
        //
        // An omission disagrees too. The foundation reads a null on a ContentItem endpoint as
        // "the narrow tier cannot be decided" and fails closed, but the derived value is known
        // here, and it governs. An honest publisher — anything that resolved the endpoints — is
        // unaffected, because for it the two values already agree.
        //
        // The same rule covers the two other values the foundation would take as handed (#631
        // criteria 3b, 3c): UserId, which it checks for length only and which makes the row
        // personal, and a VERSIONED endpoint's group id, which it keeps. A scope, and a
        // non-versioned group id, it re-derives before anything reads them, so neither is compared.
        private static void ValidateClaimsAreTheDerivation(
            Association claimedAssociation,
            Association derivedAssociation) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsNotTheDerivedContentType(
                    claimedAssociation.EntityAContentType,
                    derivedAssociation.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),
                (Rule: IsNotTheDerivedContentType(
                    claimedAssociation.EntityBContentType,
                    derivedAssociation.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),
                (Rule: IsNotTheDerivedVersionedGroupId(
                    derivedAssociation.EntityAType,
                    claimedAssociation.EntityAGroupId,
                    derivedAssociation.EntityAGroupId),
                    Parameter: nameof(Association.EntityAGroupId)),
                (Rule: IsNotTheDerivedVersionedGroupId(
                    derivedAssociation.EntityBType,
                    claimedAssociation.EntityBGroupId,
                    derivedAssociation.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),
                (Rule: IsNotTheDerivedUserId(
                    claimedAssociation.UserId,
                    derivedAssociation.UserId),
                    Parameter: nameof(Association.UserId)));

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

        private static dynamic IsNotTheDerivedContentType(
            ContentType? claimedContentType,
            ContentType? derivedContentType) => new
        {
            Condition = claimedContentType != derivedContentType,
            Message = "Value must be the content type its endpoint resolves to"
        };

        private static dynamic IsNotTheDerivedVersionedGroupId(
            EntityType entityType,
            Guid claimedGroupId,
            Guid derivedGroupId) => new
        {
            Condition = EntityTypeVersioning.IsVersioned(entityType)
                && claimedGroupId != derivedGroupId,
            Message = "Value must be the group its endpoint resolves to"
        };

        private static dynamic IsNotTheDerivedUserId(
            string? claimedUserId,
            string? derivedUserId) => new
        {
            Condition = claimedUserId != derivedUserId,
            Message = "Value is derived and must not be supplied"
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
