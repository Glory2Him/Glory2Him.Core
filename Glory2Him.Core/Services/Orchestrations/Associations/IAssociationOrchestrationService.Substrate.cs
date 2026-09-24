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

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    /// <summary>
    /// The event-facing surface of this orchestration: the ONE <c>Association</c> request address
    /// whose handler must sit above the foundation, because it derives an authorization input —
    /// <c>Entity{A,B}ContentType</c> — from two other entities' rows (#631).
    ///
    /// <para>The other seven <c>Association</c> request addresses stay bound to
    /// <c>IAssociationService</c>, deliberately. Modify, both removals, the retrieve, approve,
    /// set-confidence and set-scope derive nothing: each works from columns already on the
    /// stored row, so routing them through here would add a layer that only forwards (§ARC12.1).
    /// Set-scope re-runs the add's duplicate check, but it recomputes the effective id from the
    /// stored row rather than resolving an endpoint, which is why it does not move with this
    /// one.</para>
    ///
    /// <para>Wired to the listener exclusively in <c>EventSubscriptionRegistration</c>: the service
    /// exposes the capability, the central registration decides which tier the address binds
    /// to.</para>
    /// </summary>
    public partial interface IAssociationOrchestrationService
    {
        /// <summary>
        /// Handles an add request that arrived over the event substrate: verifies it, runs the
        /// same write flow the method path runs — the derivation and the occupancy check — refuses
        /// any claim that contradicts a derived value and any occupant of the pair, and only then
        /// hands the SAME envelope to the foundation's handler.
        ///
        /// <para>Replies with the outcome envelope the foundation produced, or <c>null</c> when the
        /// request had already been applied — the deduplication, the audit stamping, the storage
        /// write, the past-tense fact and the reply all remain the foundation's.</para>
        /// </summary>
        ValueTask<EventEnvelope<Association>?> OnAddingAssociationAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);
    }
}
