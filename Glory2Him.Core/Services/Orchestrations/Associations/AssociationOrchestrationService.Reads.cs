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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Associations;

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
    }
}
