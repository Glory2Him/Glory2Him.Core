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
using System.Collections.Generic;
using Glory2Him.Core.Models.Events.Foundations;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        public static readonly Guid LinkAddingEventAddressId =
            new Guid("019f814e-89c1-7500-be2f-3e2d3fca8125");

        public static readonly Guid LinkModifyingEventAddressId =
            new Guid("019f814e-89c1-7cf2-a539-a128df6e56a1");

        public static readonly Guid LinkRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7fa2-b64b-6f99abb496a3");

        public static readonly Guid LinkHardRemovingByIdEventAddressId =
            new Guid("019f855d-451a-7ad4-8b39-7c19fd58ef59");

        public static readonly Guid LinkRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7750-b40d-0ea26cdc9500");

        public static readonly Guid LinkAddedEventAddressId =
            new Guid("019f814e-89c1-73b4-86d8-c8dc26270d4a");

        public static readonly Guid LinkModifiedEventAddressId =
            new Guid("019f814e-89c1-7006-aa45-dba2aa66a28f");

        public static readonly Guid LinkRemovedEventAddressId =
            new Guid("019f814e-89c1-7df1-a819-13f20bd80428");

        public static readonly Guid LinkSubmittingEventAddressId =
            new Guid("249c63a8-46ff-49d1-af40-84ba5fba9385");

        public static readonly Guid LinkApprovingEventAddressId =
            new Guid("bfda8183-b318-4265-8346-d3154f10246e");

        public static readonly Guid LinkSubmittedEventAddressId =
            new Guid("0248c594-d2b8-4811-aca5-48ca361a46e3");

        public static readonly Guid LinkApprovedEventAddressId =
            new Guid("c6c447da-c14a-4af3-8923-da52f1028e8e");

        public static readonly Guid LinkRejectedEventAddressId =
            new Guid("7339041b-d413-48aa-bf27-dc63b8f50680");

        // A fact address with no request address behind it — see LinkEventOperation.
        public static readonly Guid LinkDemotedEventAddressId =
            new Guid("019ff41d-8a53-7f10-8e26-4b7c93d0a5f2");

        internal static readonly IReadOnlyDictionary<LinkEventOperation, Guid>
            LinkEventAddressIds = new Dictionary<LinkEventOperation, Guid>
            {
                { LinkEventOperation.Adding, LinkAddingEventAddressId },
                { LinkEventOperation.Modifying, LinkModifyingEventAddressId },
                { LinkEventOperation.RemovingById, LinkRemovingByIdEventAddressId },
                { LinkEventOperation.HardRemovingById, LinkHardRemovingByIdEventAddressId },
                { LinkEventOperation.RetrievingById, LinkRetrievingByIdEventAddressId },
                { LinkEventOperation.Submitting, LinkSubmittingEventAddressId },
                { LinkEventOperation.Approving, LinkApprovingEventAddressId },
                { LinkEventOperation.Added, LinkAddedEventAddressId },
                { LinkEventOperation.Modified, LinkModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("LinkHardRemoved" vs "LinkRemoved").
                { LinkEventOperation.Removed, LinkRemovedEventAddressId },
                { LinkEventOperation.HardRemoved, LinkRemovedEventAddressId },

                { LinkEventOperation.Submitted, LinkSubmittedEventAddressId },
                { LinkEventOperation.Approved, LinkApprovedEventAddressId },
                { LinkEventOperation.Rejected, LinkRejectedEventAddressId },
                { LinkEventOperation.Demoted, LinkDemotedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> LinkEventAddresses =
            new Dictionary<Guid, string>
            {
                { LinkAddingEventAddressId, "Link-Adding" },
                { LinkModifyingEventAddressId, "Link-Modifying" },
                { LinkRemovingByIdEventAddressId, "Link-RemovingById" },
                { LinkHardRemovingByIdEventAddressId, "Link-HardRemovingById" },
                { LinkRetrievingByIdEventAddressId, "Link-RetrievingById" },
                { LinkSubmittingEventAddressId, "Link-Submitting" },
                { LinkApprovingEventAddressId, "Link-Approving" },
                { LinkAddedEventAddressId, "Link-Added" },
                { LinkModifiedEventAddressId, "Link-Modified" },
                { LinkRemovedEventAddressId, "Link-Removed" },
                { LinkSubmittedEventAddressId, "Link-Submitted" },
                { LinkApprovedEventAddressId, "Link-Approved" },
                { LinkRejectedEventAddressId, "Link-Rejected" },
                { LinkDemotedEventAddressId, "Link-Demoted" }
            };

        public static readonly Guid LinkOnAddingLinkSubscriptionId =
            new Guid("019f8170-a642-7619-927b-3bd82e116681");

        public const string LinkOnAddingLinkSubscriptionName =
            "LinkService.OnAddingLink";
        public static readonly Guid LinkOnModifyingLinkSubscriptionId =
            new Guid("019f8170-a642-7bb9-8c3b-fc6c5e8523e5");

        public const string LinkOnModifyingLinkSubscriptionName =
            "LinkService.OnModifyingLink";
        public static readonly Guid LinkOnRemovingLinkByIdSubscriptionId =
            new Guid("019f8170-a642-7791-a7f2-b0a4c3fb609a");

        public const string LinkOnRemovingLinkByIdSubscriptionName =
            "LinkService.OnRemovingLinkById";
        public static readonly Guid LinkOnHardRemovingLinkByIdSubscriptionId =
            new Guid("019f855d-451b-7603-8325-d17f43c3fb04");

        public const string LinkOnHardRemovingLinkByIdSubscriptionName =
            "LinkService.OnHardRemovingLinkById";

        public static readonly Guid LinkOnRetrievingLinkByIdSubscriptionId =
            new Guid("019f8170-a642-799e-8b21-57b457c2be45");

        public const string LinkOnRetrievingLinkByIdSubscriptionName =
            "LinkService.OnRetrievingLinkById";

        public static readonly Guid LinkOnSubmittingLinkSubscriptionId =
            new Guid("8e581e87-9ecb-4631-bc66-c6526b75a691");

        public const string LinkOnSubmittingLinkSubscriptionName =
            "LinkService.OnSubmittingLink";

        public static readonly Guid LinkOnApprovingLinkSubscriptionId =
            new Guid("fe4b9584-c848-4fa2-9e54-741ed85ce7b9");

        public const string LinkOnApprovingLinkSubscriptionName =
            "LinkService.OnApprovingLink";

        // Demote has no subscription and no request address (see LinkEventOperation). This name
        // exists only as the ProcessedEvents receiver, the same way sort's does on Association.
        public const string LinkOnDemotingLinkSubscriptionName =
            "LinkService.OnDemotingLink";
    }
}
