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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial class LinkProcessingService
    {
        public ValueTask<EventEnvelope<Link>?> OnAddingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.Adding);

                Link addedLink =
                    await DoAddLinkAsync(
                        link: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: addedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnModifyingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.Modifying);

                Link modifiedLink =
                    await DoModifyLinkAsync(
                        link: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: modifiedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnRemovingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.RemovingById);

                Link removedLink =
                    await DoRemoveLinkByIdAsync(
                        linkId: envelope.Content.Id,
                        deletionReason: envelope.Content.DeletionReason,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: removedLink);
            });

        public ValueTask<EventEnvelope<Link>?> OnApprovingLinkAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.Approving);

                Link decidedLink =
                    await DoTransitionLinkApprovalAsync(
                        command: envelope.Content,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                // This service's OWN completion fact, distinct from the foundation's
                // Link-Approved: that one says a row was decided, this one says the
                // GROUP was left consistent — the incumbent cleared and the new row
                // promoted. A subscriber that needs the second cannot infer it from the
                // first, because the foundation fact is published before this process
                // has finished (§10.2 rule 5).
                await PublishLinkProcessingFactAsync(
                    inboundEnvelope: envelope,
                    link: decidedLink,
                    operation: LinkProcessingEventOperation.Approved);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: decidedLink);
            });

        // The publication swap (§9.7.7 rules 6–7, §12.4.1 rule 10). Two rows of one entity
        // in a guaranteed order, which is why it lives here rather than in the foundation (one
        // row per call) or the orchestration (no entity services, by design).
        //
        // The order is guaranteed by the CALL STACK — two sequential awaits in one method —
        // not by delivery. Re-publishing the two writes as events would lose it: handler
        // failures are recorded per listener rather than failing the publisher, so a failed
        // demote would not stop the promote, and the promote is the one the index refuses.
        private async ValueTask<Link> DoTransitionLinkApprovalAsync(
            Link command,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // Only a PROMOTION needs the group cleared. A rejection, a re-open or an override
            // takes nothing into the published slot, so no probe runs and the command goes
            // straight through to the foundation.
            bool isPromotion =
                command.ApprovalStatus == ApprovalStatus.Approved
                    && command.IsPublished;

            if (isPromotion)
            {
                await DemoteIncumbentPublishedLinkAsync(
                    linkId: command.Id,
                    inboundEnvelope: inboundEnvelope,
                    cancellationToken: cancellationToken);
            }

            // The envelope is FORWARDED here too, not just to the unpublish. Without it
            // the promote is re-authorised against the ambient caller — who on an
            // automatic approval is the reviewer whose own review completed the round,
            // and whose approval row is by now no longer Submitted, so the decision
            // function refuses it deterministically (§16.7.1).
            return await this.linkService.TransitionLinkApprovalAsync(
                link: command,
                inboundEnvelope: inboundEnvelope,
                cancellationToken: cancellationToken);
        }

        // Clears the group's published slot, if anything holds it.
        private async ValueTask DemoteIncumbentPublishedLinkAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // ONE gated probe, not a caller-filtered read plus a group lookup. The swap
            // acts on the workflow's system identity, which CreateSystemAsync leaves with no
            // roles and a SubjectId that is the SYSTEM rather than the row's owner.
            // Passed to the caller-facing read that identity is refused outright — the row is
            // mid-promotion, so not publicly visible, and the actor is neither owner nor
            // review-role holder. Forwarding a correct identity into a filtered read is what
            // breaks; the write flow needs a gated probe instead (#291, design §14.6).
            //
            // The group is still resolved from the STORED row: a caller-supplied GroupId would
            // let one group's approval unpublish another group's live row. The probe stays
            // UNFILTERED on the incumbent side. The slot index no longer counts deleted rows
            // (§3.4.1), so a tombstone cannot block the promote any more — but it can still be
            // CARRYING IsPublished, and a row claiming to be the group's published version
            // while invisible to every read is exactly what §9.7.6 rule 1 forbids. Clearing it
            // here keeps the flag and the index telling the same story (§9.7.7 rule 7).
            Guid? incumbentId =
                await this.linkService.FindPublishedSiblingLinkIdAsync(
                    linkId: linkId,
                    inboundEnvelope: inboundEnvelope,
                    cancellationToken: cancellationToken);

            if (incumbentId is null)
            {
                return;
            }

            // Not caught. If the slot cannot be cleared the promote must not be attempted: the
            // index would refuse it anyway, and failing here leaves the incumbent published
            // rather than the group dark.
            await this.linkService.UnpublishLinkByIdAsync(
                linkId: incumbentId.Value,
                inboundEnvelope: inboundEnvelope,
                cancellationToken: cancellationToken);
        }

        public ValueTask<EventEnvelope<Link>?> OnRetrievingLinkByIdAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatchSubstrate(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ValidateLinkEventEnvelopeAsync(
                    envelope, LinkProcessingEventOperation.RetrievingById);

                // read-only: naturally idempotent and publishes no completion fact — the
                // reply envelope is the whole outcome
                Link retrievedLink =
                    await DoRetrieveLinkByIdAsync(
                        linkId: envelope.Content.Id,
                        inboundEnvelope: envelope,
                        cancellationToken: cancellationToken);

                return await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: envelope,
                    content: retrievedLink);
            });
    }
}
