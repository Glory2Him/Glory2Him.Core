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

namespace Glory2Him.Core.Services.Foundations.Associations
{
    /// <summary>
    /// The event-facing surface of the service: request handlers invoked by the event
    /// substrate, one per request address. These are wired to event listeners exclusively in
    /// <c>EventSubscriptionRegistration</c> — the service exposes the capability; the central
    /// registration decides what is connected. Every handler replies with the operation's
    /// outcome envelope (recorded on the delivery), or <c>null</c> when a duplicated request
    /// was skipped.
    ///
    /// <para><b><c>OnAddingAssociationAsync</c> is no longer reached from the registration.</b>
    /// That address binds <c>IAssociationOrchestrationService</c>, which derives both endpoints
    /// and their content types and then calls this handler with the same envelope (#631). The
    /// capability stays here — the deduplication, the write, the canonical ordering, the fact and
    /// the reply are all this service's — but the address is one tier up, because the rule it
    /// carries reads two other entities' rows and a foundation may not.</para>
    /// </summary>
    internal partial interface IAssociationService
    {
        ValueTask<EventEnvelope<Association>?> OnAddingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this add request has ALREADY been applied by this receiver — the same
        /// <c>ProcessedEvents</c> question <see cref="OnAddingAssociationAsync"/> asks itself,
        /// exposed so the layer above can ask it FIRST.
        ///
        /// <para>The orchestration that now owns <c>Association-Adding</c> reads BOTH endpoint rows
        /// before it delegates, to derive <c>Entity{A,B}ContentType</c> (#631). Left after the
        /// deduplication, those reads make a replay do more than it used to: a re-delivered
        /// envelope whose endpoint has since been soft-deleted, or has stopped being visible to
        /// the signed caller, fails the derivation and is recorded as a failed delivery and
        /// retried — for an event that was already applied. Before the address moved up a tier a
        /// duplicate short-circuited to <c>null</c> without touching either endpoint, and it must
        /// still.</para>
        ///
        /// <para>A boolean, over the receiver's own bookkeeping — it reveals nothing but whether
        /// this system has seen an event id the caller minted. The handler keeps asking the same
        /// question for itself, because it must be safe called alone (§SEC14.6 rule 1); this only
        /// moves the answer earlier for the path that has work in front of it.</para>
        /// </summary>
        internal ValueTask<bool> HasAlreadyAddedAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnModifyingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnRemovingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnHardRemovingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnRetrievingAssociationByIdAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        // One handler per request address, including the state transitions. Sort is the only
        // one whose direct-call signature takes more than the entity, and the event path has
        // no way to carry an anchor and a side — so it is deliberately absent here rather than
        // faked. See the note on the sort handler in AssociationService.Substrate.cs.

        ValueTask<EventEnvelope<Association>?> OnApprovingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        // The bypass reason rides on the envelope's entity, in the field the outcome is
        // recorded in. That is not the field being accepted as input: what lands on the row is
        // still derived from the verdict, and is cleared when the verdict waived nothing.
        ValueTask<EventEnvelope<Association>?> OnSettingAssociationConfidenceAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnSettingAssociationScopeAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);
    }
}
