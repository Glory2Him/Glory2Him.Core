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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        public ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The foundation's own filter — §SEC14.3 rules 1, 2 and 5, plus the §SEC14.7
                // posture A′ caller clause. It is NOT re-tested above; this read composes on top
                // of it and adds only what spans the endpoints.
                IQueryable<Association> visibleAssociations =
                    await this.associationService.RetrieveAllAssociationsAsync(cancellationToken);

                ResolvedEndpointVisibility visibleEndpoints =
                    await ResolveCallerVisibleEndpointsAsync(cancellationToken);

                return ApplyEndpointVisibilityComposite(
                    associations: visibleAssociations,
                    visibleEndpoints: visibleEndpoints);
            });

        public ValueTask<Association> RetrieveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationId(associationId);

                // The foundation answers the first three misses — no row, a soft-deleted row, and
                // a row this caller's posture hides — all of them as not-found, carrying no
                // reason (§SEC14.5 rules 1 and 2).
                Association maybeAssociation =
                    await this.associationService.RetrieveAssociationByIdAsync(
                        associationId,
                        cancellationToken);

                await ValidateEndpointsAreVisibleAsync(maybeAssociation, cancellationToken);

                return maybeAssociation;
            });

        // The fourth miss: the row is visible but one of its endpoints is not. One row is in
        // hand, so the two endpoints are resolved DIRECTLY rather than composed — which is a
        // statement about COST, two reads instead of a set-keyed one, and never a licence for a
        // second predicate.
        //
        // WHICH ROW IS ASKED ABOUT MUST MATCH THE COMPOSITE TERM FOR TERM. §SEC14.3: two
        // resolvers are allowed, two predicates are not. A collection read matching the group
        // while the single read of the same row matches the key id shows up as a row that is in
        // the list and not found when it is opened — and that is not a theory, it is what this
        // method did before #657's second round.
        //
        // The message is normalised to the association's own not-found — the SAME message the
        // foundation answers its three misses with, because §SEC14.5 rule 1 measures what an
        // unprivileged probe can tell apart and a probe sees a body. Naming an endpoint here
        // would report one of the row's columns and make this miss distinguishable from the other
        // four (rules 1 and 2). On the ADD path naming it costs nothing, because the caller
        // supplied those ids themselves.
        //
        // The id in the message is the caller's own input and is none of the three things rule 2
        // bars. The two exception FAMILIES stay two — this one and the foundation's — because
        // they record which layer refused, which is a different question from what the caller is
        // told.
        private async ValueTask ValidateEndpointsAreVisibleAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            try
            {
                await ValidateEndpointIsVisibleAsync(
                    entityType: association.EntityAType,
                    keyId: association.EntityAKeyId,
                    groupId: association.EntityAGroupId,
                    scope: association.EntityAScope,
                    endpointName: "A",
                    cancellationToken: cancellationToken);

                await ValidateEndpointIsVisibleAsync(
                    entityType: association.EntityBType,
                    keyId: association.EntityBKeyId,
                    groupId: association.EntityBGroupId,
                    scope: association.EntityBScope,
                    endpointName: "B",
                    cancellationToken: cancellationToken);
            }
            catch (NotFoundAssociationOrchestrationException)
            {
                throw new NotFoundAssociationOrchestrationException(
                    message: $"Content item association not found with id: {association.Id}.");
            }

            // AN ENDPOINT TYPE WITH NO FOUNDATION SERVICE IS A NOT-FOUND ON THIS PATH, AND AN
            // ORDINARY VALIDATION FAILURE ON THE ADD. The test is who supplied the value. On the
            // add the caller named the endpoint type, so refusing it by name discloses only their
            // own input; here they supplied an association id and nothing else, so the same
            // sentence confirms the row exists and reports one of its columns — the entity's
            // state under §SEC14.5 rule 2 — and makes this miss one a probe can tell from the
            // other four under rule 1.
            //
            // It is also the right ANSWER rather than only the safe wording: there is no read
            // that could show such an endpoint visible, so §SEC14.3 rule 4 cannot be satisfied,
            // and an undecidable visibility input fails closed. The collection read already drops
            // the row for exactly that reason, and the two paths agree.
            catch (InvalidAssociationOrchestrationException)
            {
                throw new NotFoundAssociationOrchestrationException(
                    message: $"Content item association not found with id: {association.Id}.");
            }
        }

        // The READ paths' resolver, through the SAME conversion the add's resolver uses: an
        // endpoint service's validation failure is an unresolved endpoint, never a dependency
        // error.
        private ValueTask ValidateEndpointIsVisibleAsync(
            EntityType entityType,
            Guid keyId,
            Guid groupId,
            Scope scope,
            string endpointName,
            CancellationToken cancellationToken) =>
            ConvertEndpointValidationFailureToNotFoundAsync(
                resolveEndpointAsync: () => ResolveVisibleEndpointCoreAsync(
                    entityType, keyId, groupId, scope, cancellationToken),
                endpointName: endpointName);

        // THE PREDICATE, and it is the composite's own, stated the other way round. The two
        // must agree term for term:
        //
        //   ContentItem / Link at AllVersions      -> the GROUP           (EntityXGroupId)
        //   ContentItem / Link at ThisVersionOnly  -> the ROW             (EntityXKeyId)
        //   every other endpoint type              -> the ROW, whatever the scope column says
        //
        // The last line is not a shortcut: only a versioned type can legitimately be written
        // AllVersions, so a non-versioned endpoint carrying it is bad data, and answering it at a
        // "group" that is its own id would widen nothing but would differ from the composite,
        // which ignores the scope column for those four types.
        //
        // Nothing enforces the agreement structurally — one side is an expression tree over
        // queryables and the other is a pair of direct calls, and they cannot share code. What
        // enforces it is criterion 3's cross-path pair, which runs both reads over one world and
        // reds the moment they disagree.
        private async ValueTask ResolveVisibleEndpointCoreAsync(
            EntityType entityType,
            Guid keyId,
            Guid groupId,
            Scope scope,
            CancellationToken cancellationToken)
        {
            switch (entityType)
            {
                case EntityType.ContentItem when scope == Scope.AllVersions:
                    IReadOnlyList<ContentItem> groupContentItems =
                        await this.contentItemService.RetrieveContentItemsByGroupIdAsync(
                            groupId, cancellationToken);

                    ValidateGroupHasAVisibleVersion(groupContentItems.Count);

                    return;

                case EntityType.Link when scope == Scope.AllVersions:
                    IReadOnlyList<Link> groupLinks =
                        await this.linkService.RetrieveLinksByGroupIdAsync(
                            groupId, cancellationToken);

                    ValidateGroupHasAVisibleVersion(groupLinks.Count);

                    return;

                default:
                    // The row, through the endpoint's own by-id read — including the two types
                    // above at ThisVersionOnly. An endpoint type with no foundation service
                    // raises InvalidAssociationOrchestrationException here, which the caller of
                    // this method turns into the association's own not-found.
                    await ResolveEndpointCoreAsync(
                        entityType, keyId, readEnvelope: null, cancellationToken);

                    return;
            }
        }

        // The group-keyed read carries its foundation's §SEC14.7 posture unchanged and returns no
        // tombstone, so an empty slice means the group holds nothing this caller may see — which
        // is the group-level answer to §SEC14.3 rule 4, exactly as the composite's
        // GroupId == EntityXGroupId term is.
        private static void ValidateGroupHasAVisibleVersion(int visibleVersionCount)
        {
            if (visibleVersionCount == 0)
            {
                throw new NotFoundAssociationOrchestrationException(
                    message: "The endpoint's version group holds no visible version.");
            }
        }
    }
}
