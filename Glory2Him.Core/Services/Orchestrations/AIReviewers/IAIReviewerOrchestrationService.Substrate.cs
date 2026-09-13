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
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// The event-facing surface of Berean's orchestration (design §8.6.2.1) — the automatic
    /// assignment, which is a REACTION rather than an operation and so appears nowhere on the
    /// caller-facing contract's three verbs.
    ///
    /// <para>Bound to their addresses exclusively in <c>EventSubscriptionRegistration</c>, which
    /// resolves each address from <c>EventBrokerIdentifiers</c>' operation-to-address map. The
    /// service declares the capability; the central registration decides what it listens
    /// on.</para>
    ///
    /// <para>Both reply <c>null</c>. A fact is a notification, so there is nothing to reply with
    /// — the responder shape is here only because the delivery records one.</para>
    /// </summary>
    public partial interface IAIReviewerOrchestrationService
    {
        /// <summary>
        /// §8.6.2.1 — a round OPENED. Where it opened already at <c>Submitted</c> and every gate
        /// passes, Berean is assigned to it under the system identity, with nobody having asked.
        /// A round opened at <c>Draft</c> is picked up later, by the <c>-Modified</c> its
        /// submission publishes.
        /// </summary>
        ValueTask<EventEnvelope<Approval>?> OnApprovalAddedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// §8.6.2.1 — a round may have REACHED <c>Submitted</c> after it opened: the submission
        /// of a round opened at <c>Draft</c>, or §8.6 HR-4's reset re-opening a decided one. One
        /// address hears every route, because all of them write through
        /// <c>ModifyApprovalAsync</c>.
        ///
        /// <para>Same gates, same write, same body — only the accepted event name differs from
        /// its sibling above.</para>
        /// </summary>
        ValueTask<EventEnvelope<Approval>?> OnApprovalModifiedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default);
    }
}
