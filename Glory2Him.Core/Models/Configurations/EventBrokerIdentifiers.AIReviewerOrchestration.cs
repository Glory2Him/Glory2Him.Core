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
        // Subscription identifiers only — the AI reviewer orchestration owns no event address of
        // its own, exactly as the reviewer orchestration's partial says of itself. It listens on
        // two addresses the Approval foundation publishes and causes its write through that
        // foundation's workflow seam, which publishes the ordinary AIReviewerAssignment-Added for
        // itself, so there is nothing here to register in the address map and nothing to add to
        // entityEventAddresses.
        //
        // NO REQUEST ADDRESS, and that is a ruling rather than an omission (§8.6.2.1): an
        // automatic assignment is not something a caller may ask for, and the act it performs is
        // already exposed to callers at AIReviewerAssignment-Adding. There is no -ing verb to
        // justify under §EVN2 rule 7.
        //
        // TWO subscriptions, both for §8.6.2.1's automatic assignment and neither for a re-test.
        // Two addresses rather than one because a round can open AT Submitted or arrive there
        // later, and no single address hears both: Approval-Added at Submitted is §9.7.2 rule 1's
        // create-at-Submitted case, and every other route to Submitted writes through
        // ModifyApprovalAsync and lands on Approval-Modified.
        //
        // THE IDS ARE MINTED FOR THESE TWO and share nothing with the reviewer orchestration's,
        // which binds Approval-Modified as well. Two subscriptions carrying one id collapse to a
        // single registration and one of the two handlers is silently dropped — no error, no log,
        // just a reaction that stops happening.
        //
        // Approval-Modified therefore has TWO subscribers: §7.9 rule 8's retirement and this
        // assignment. That is not the double-fire §EVN2 rule 6 forbids, which bars one reaction
        // from binding both the foundation and the layer tier of the same fact; here there are two
        // reactions on one address, in two services, with Deliveries recorded per subscription
        // (§EVN11). The two must not be merged either: the retirement acts where the signed status
        // says the round CLOSED and this one where it says the round is OPEN, so the gates are
        // disjoint by construction and no delivery reaches both bodies.
        //
        // Approval-Added gains its first subscriber of any kind.

        public static readonly Guid
            AIReviewerOrchestrationOnApprovalAddedSubscriptionId =
                new Guid("6f16d951-4472-4fd0-81dc-3ad9e004a34c");

        public const string AIReviewerOrchestrationOnApprovalAddedSubscriptionName =
            "AIReviewerOrchestrationService.OnApprovalAdded";

        public static readonly Guid
            AIReviewerOrchestrationOnApprovalModifiedSubscriptionId =
                new Guid("e7f4ed72-baa8-4c82-904d-8507c94fc3d0");

        public const string AIReviewerOrchestrationOnApprovalModifiedSubscriptionName =
            "AIReviewerOrchestrationService.OnApprovalModified";
    }
}
