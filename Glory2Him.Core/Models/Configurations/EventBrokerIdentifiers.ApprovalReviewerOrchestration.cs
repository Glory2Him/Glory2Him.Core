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

using System;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        // Subscription identifiers only — the reviewer orchestration owns no event address of its
        // own, exactly as the approval round's partial says of itself. It listens on two addresses
        // other services publish and causes its writes through the foundation's workflow seam,
        // which publishes for itself, so there is nothing here to register in the address map and
        // nothing to add to entityEventAddresses.
        //
        // TWO subscriptions, both for §7.9's retirements and neither for a re-test (§EVN18's
        // reviewer table, G2H Design.md §12.5.4 business rule 4).
        //
        // THE IDS ARE MINTED FOR THESE TWO and share nothing with the round's service. Two
        // subscriptions carrying one id collapse to a single registration and one of the two
        // handlers is silently dropped — no error, no log, just a reaction that stops happening —
        // which is the specific accident reusing an id from the service beside this one would
        // cause.
        //
        // ApprovalReview-Added therefore has TWO subscribers: the round's re-test and this
        // retirement. That is not the double-fire §EVN2 rule 6 forbids, which bars one reaction
        // from binding both the foundation and the layer tier of the same fact; here there are two
        // reactions on one address, in two services, with Deliveries recorded per subscription
        // (§EVN11).
        //
        // Approval-Modified is the FIRST subscription in the solution on any of the Approval
        // entity's own fact addresses — the five SubscribeToApprovalEventAsync registrations all
        // bind command addresses. It is admissible under §EVN18(e)'s amended boundary precisely
        // because this handler re-tests nothing: it reads no §8.5 predicate, moves none, and
        // decides nothing.

        public static readonly Guid
            ApprovalReviewerOrchestrationOnApprovalReviewAddedSubscriptionId =
                new Guid("01a096e5-484b-7524-9246-f29da23962ce");

        public const string ApprovalReviewerOrchestrationOnApprovalReviewAddedSubscriptionName =
            "ApprovalReviewerOrchestrationService.OnApprovalReviewAdded";

        public static readonly Guid
            ApprovalReviewerOrchestrationOnApprovalModifiedSubscriptionId =
                new Guid("01a096e5-484b-7c48-b633-44b02cf9c228");

        public const string ApprovalReviewerOrchestrationOnApprovalModifiedSubscriptionName =
            "ApprovalReviewerOrchestrationService.OnApprovalModified";
    }
}
