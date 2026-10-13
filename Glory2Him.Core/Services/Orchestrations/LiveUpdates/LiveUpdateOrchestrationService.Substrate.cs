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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    internal partial class LiveUpdateOrchestrationService
    {
        // The forwarder's ears. Each verifies the fact against the name its address carries before
        // anything is read, runs the body for its kind of fact — the reactions body on the
        // verified row, the settings body on nothing, since it reads nothing — and answers null:
        // nothing replies to a fact (§EVN20). None drops a fact that carries the system
        // identity, unlike the approval workflow's ears — the workflow's approval is exactly the
        // change a page has to hear (§EVN26 rule 3).
        public ValueTask<EventEnvelope<Association>?> OnAssociationApprovedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Association>?>(async () =>
            {
                // ahead of the verify: a caller who has already cancelled should not pay for an
                // HMAC computation, let alone the reads
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateAssociationFactEnvelopeAsync(
                    envelope,
                    eventName: "AssociationApproved");

                await SendReactionsChangeAsync(envelope.Content, cancellationToken);

                return null;
            });

        public ValueTask<EventEnvelope<Association>?> OnAssociationRejectedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Association>?>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateAssociationFactEnvelopeAsync(
                    envelope,
                    eventName: "AssociationRejected");

                await SendReactionsChangeAsync(envelope.Content, cancellationToken);

                return null;
            });

        // The approval workflow's ear hears this address too, and the two rely on no order
        // between them (§EVN26 rule 5).
        public ValueTask<EventEnvelope<Association>?> OnAssociationSubmittedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Association>?>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateAssociationFactEnvelopeAsync(
                    envelope,
                    eventName: "AssociationSubmitted");

                await SendReactionsChangeAsync(envelope.Content, cancellationToken);

                return null;
            });

        public ValueTask<EventEnvelope<Association>?> OnAssociationRemovedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            throw new System.NotImplementedException();

        public ValueTask<EventEnvelope<ContentItemSetting>?> OnContentItemSettingAddedAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<ContentItemSetting>?>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ValidateContentItemSettingFactEnvelopeAsync(
                    envelope,
                    eventName: "ContentItemSettingAdded");

                await SendSettingsChangeAsync(cancellationToken);

                return null;
            });
    }
}
