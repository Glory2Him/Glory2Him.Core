// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// The event-facing surface of the reviewer coordination (§12.5.4 business rule 4) — the two
    /// §7.9 retirements, which are REACTIONS rather than operations and so appear nowhere on the
    /// caller-facing contract's five verbs.
    ///
    /// <para>Bound to their addresses exclusively in <c>EventSubscriptionRegistration</c>, which
    /// resolves each address from <c>EventBrokerIdentifiers</c>' operation-to-address map. The
    /// service declares the capability; the central registration decides what it listens
    /// on.</para>
    ///
    /// <para>Both reply <c>null</c>. A fact is a notification, so there is nothing to reply with
    /// — the responder shape is here only because the delivery records one.</para>
    /// </summary>
    public partial interface IApprovalReviewerOrchestrationService
    {
        /// <summary>
        /// §7.9 rule 6 — a review has been recorded, so if the person who recorded it was one
        /// the round had invited, their invitation is retired under the system identity.
        /// Silent where the reviewer was never asked, which is the common case.
        /// </summary>
        ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewAddedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default);
    }
}
