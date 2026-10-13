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

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        // Subscription identifiers only. The forwarder owns no event address of its own: a live
        // update is caused only by a fact a service already publishes, so nothing is minted for a
        // page's sake and there is nothing here to register in the address map (§EVN26 rule 1).
        //
        // THE IDS ARE MINTED FOR THESE SUBSCRIPTIONS and share nothing with any other
        // subscriber's on the same address. Two subscriptions carrying one id collapse to a
        // single registration and one of the two handlers is silently dropped.

        public static readonly Guid LiveUpdateOrchestrationOnAssociationApprovedSubscriptionId =
            new Guid("01a12ccf-d293-7867-b3d3-2acb7d944bbb");

        public const string LiveUpdateOrchestrationOnAssociationApprovedSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationApproved";

        public static readonly Guid LiveUpdateOrchestrationOnAssociationRejectedSubscriptionId =
            new Guid("50791ded-4a06-459d-a1ae-f7ea9e948bb7");

        public const string LiveUpdateOrchestrationOnAssociationRejectedSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationRejected";

        public static readonly Guid LiveUpdateOrchestrationOnAssociationSubmittedSubscriptionId =
            new Guid("c3f01354-adee-40aa-bf72-79722741c9c0");

        public const string LiveUpdateOrchestrationOnAssociationSubmittedSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationSubmitted";

        public static readonly Guid LiveUpdateOrchestrationOnAssociationRemovedSubscriptionId =
            new Guid("b66e11c1-7878-4da0-a399-b3244e2614bc");

        public const string LiveUpdateOrchestrationOnAssociationRemovedSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationRemoved";

        public static readonly Guid LiveUpdateOrchestrationOnAssociationRestoredSubscriptionId =
            new Guid("a4d6fb22-26cf-4ee8-ad59-1be814252f2e");

        public const string LiveUpdateOrchestrationOnAssociationRestoredSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationRestored";

        public static readonly Guid LiveUpdateOrchestrationOnAssociationRepointedSubscriptionId =
            new Guid("5bfe0660-dcd1-4259-9168-d077e3670c86");

        public const string LiveUpdateOrchestrationOnAssociationRepointedSubscriptionName =
            "LiveUpdateOrchestrationService.OnAssociationRepointed";

        public static readonly Guid LiveUpdateOrchestrationOnContentItemSettingAddedSubscriptionId =
            new Guid("337b7647-25e3-46b1-9ef3-afe39fcd5c3e");

        public const string LiveUpdateOrchestrationOnContentItemSettingAddedSubscriptionName =
            "LiveUpdateOrchestrationService.OnContentItemSettingAdded";
    }
}
