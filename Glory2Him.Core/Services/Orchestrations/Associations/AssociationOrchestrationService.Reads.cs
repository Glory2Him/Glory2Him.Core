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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Associations;
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
        // hand, so the two endpoints are resolved DIRECTLY — there is nothing here for a query
        // composition to save.
        //
        // ResolveEndpointAsync is reused rather than re-derived, so the conversion of an endpoint
        // service's validation failure into a not-found is written once (§ARC16.8). Letting it
        // reach the closing catch (Xeption) arm instead would answer "this endpoint is not
        // visible" with a 424 — a visibility rule reported as a failed dependency, leaking
        // through the status code exactly what §SEC14.5 rule 2 keeps out of the message.
        //
        // The message is normalised to the association's own not-found. On the ADD path naming
        // the endpoint costs nothing, because the caller supplied those ids themselves; here they
        // supplied only an association id, so naming an endpoint would tell them WHY the row was
        // refused and make this miss distinguishable from the other three.
        private async ValueTask ValidateEndpointsAreVisibleAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            try
            {
                await ResolveEndpointAsync(
                    association.EntityAType,
                    association.EntityAKeyId,
                    onResolved: _ => { },
                    endpointName: "A",
                    cancellationToken: cancellationToken);

                await ResolveEndpointAsync(
                    association.EntityBType,
                    association.EntityBKeyId,
                    onResolved: _ => { },
                    endpointName: "B",
                    cancellationToken: cancellationToken);
            }
            catch (NotFoundAssociationOrchestrationException)
            {
                throw new NotFoundAssociationOrchestrationException(
                    message: "Content item association not found.");
            }
        }
    }
}
