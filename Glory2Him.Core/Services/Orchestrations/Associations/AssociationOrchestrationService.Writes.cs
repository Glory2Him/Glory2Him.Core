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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // MODIFY COMPOSES NOTHING, AND THAT IS THE DESIGN RATHER THAN AN OMISSION. It is the
        // row-free half of the §SEC14.7 posture A′ rule 4 gate plus a forward to the foundation.
        // Authorization here is decided against the STORED endpoints and never the caller's copy,
        // so a caller-supplied Association puts no trustworthy endpoint in this layer's hand —
        // which is why modify sits with the two removals rather than with the add. Adding
        // endpoint resolution to this member is a finding, not an improvement.
        //
        // It exists because §EVN13 rule 3 binds an exposer to the entity's top-layer service
        // while the exposer guidance caps a controller at one injected service, not because there
        // is anything here to orchestrate.
        public ValueTask<Association> ModifyAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                ValidateUserMayWriteWithoutTheStoredRow(envelope.SecurityContext);

                return await this.associationService.ModifyAssociationAsync(
                    association,
                    cancellationToken);
            });

        // The reversible takedown, and the same split as modify. The orchestration runs
        // authentication and the global block BEFORE anything touches the Associations table,
        // which is what keeps this surface from being used to probe which association ids exist;
        // the owner-or-Administrators test and both ends of the veto are composed from the stored
        // row and belong to the foundation (§SEC14.7 posture A′ rule 4). No second read is issued
        // to duplicate them.
        //
        // The deletion reason is forwarded verbatim rather than dropped: it is the audit stamp of
        // WHY a row came down, and this is the only layer #318's controller can reach it through.
        public ValueTask<Association> RemoveAssociationByIdAsync(
            Guid associationId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the row-free half
                // of the gate runs against — the request payload carries only the id and reason
                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(
                        content: new Association
                        {
                            Id = associationId,
                            DeletionReason = deletionReason,
                        });

                ValidateUserMayWriteWithoutTheStoredRow(envelope.SecurityContext);
                ValidateAssociationId(associationId);

                return await this.associationService.RemoveAssociationByIdAsync(
                    associationId,
                    deletionReason,
                    cancellationToken);
            });
    }
}
