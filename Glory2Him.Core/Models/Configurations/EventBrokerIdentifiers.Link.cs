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
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid LinkAddingEventAddressId =
            new Guid("019f814e-89c1-7500-be2f-3e2d3fca8125");

        public static readonly Guid LinkModifyingEventAddressId =
            new Guid("019f814e-89c1-7cf2-a539-a128df6e56a1");

        public static readonly Guid LinkRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7fa2-b64b-6f99abb496a3");

        public static readonly Guid LinkRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7750-b40d-0ea26cdc9500");

        public static readonly Guid LinkAddedEventAddressId =
            new Guid("019f814e-89c1-73b4-86d8-c8dc26270d4a");

        public static readonly Guid LinkModifiedEventAddressId =
            new Guid("019f814e-89c1-7006-aa45-dba2aa66a28f");

        public static readonly Guid LinkRemovedEventAddressId =
            new Guid("019f814e-89c1-7df1-a819-13f20bd80428");

        internal static readonly IReadOnlyDictionary<LinkEventOperation, Guid>
            LinkEventAddressIds = new Dictionary<LinkEventOperation, Guid>
            {
                { LinkEventOperation.Adding, LinkAddingEventAddressId },
                { LinkEventOperation.Modifying, LinkModifyingEventAddressId },
                { LinkEventOperation.RemovingById, LinkRemovingByIdEventAddressId },
                { LinkEventOperation.RetrievingById, LinkRetrievingByIdEventAddressId },
                { LinkEventOperation.Added, LinkAddedEventAddressId },
                { LinkEventOperation.Modified, LinkModifiedEventAddressId },
                { LinkEventOperation.Removed, LinkRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> LinkEventAddresses =
            new Dictionary<Guid, string>
            {
                { LinkAddingEventAddressId, "Link-Adding" },
                { LinkModifyingEventAddressId, "Link-Modifying" },
                { LinkRemovingByIdEventAddressId, "Link-RemovingById" },
                { LinkRetrievingByIdEventAddressId, "Link-RetrievingById" },
                { LinkAddedEventAddressId, "Link-Added" },
                { LinkModifiedEventAddressId, "Link-Modified" },
                { LinkRemovedEventAddressId, "Link-Removed" }
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
        public static readonly Guid LinkOnRetrievingLinkByIdSubscriptionId =
            new Guid("019f8170-a642-799e-8b21-57b457c2be45");

        public const string LinkOnRetrievingLinkByIdSubscriptionName =
            "LinkService.OnRetrievingLinkById";
    }
}
