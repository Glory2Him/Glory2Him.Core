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
        public static readonly Guid ContentItemAssociationAddingEventAddressId =
            new Guid("019f814e-89c1-7478-a47c-e4f34ca752c2");

        public static readonly Guid ContentItemAssociationModifyingEventAddressId =
            new Guid("019f814e-89c1-73ae-b402-3079db44172d");

        public static readonly Guid ContentItemAssociationRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-74c4-92b6-f9578ee44c11");

        public static readonly Guid ContentItemAssociationHardRemovingByIdEventAddressId =
            new Guid("019f855d-4516-7bab-8ed1-00765c36a128");

        public static readonly Guid ContentItemAssociationRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7882-b3a5-7fc91878d31b");

        public static readonly Guid ContentItemAssociationAddedEventAddressId =
            new Guid("019f814e-89c1-7ac2-b650-d11879294256");

        public static readonly Guid ContentItemAssociationModifiedEventAddressId =
            new Guid("019f814e-89c1-7adf-9529-4b021ea0c22e");

        public static readonly Guid ContentItemAssociationRemovedEventAddressId =
            new Guid("019f814e-89c1-7e44-8fe2-bd2d85f21c3d");

        internal static readonly IReadOnlyDictionary<ContentItemAssociationEventOperation, Guid>
            ContentItemAssociationEventAddressIds = new Dictionary<ContentItemAssociationEventOperation, Guid>
            {
                { ContentItemAssociationEventOperation.Adding, ContentItemAssociationAddingEventAddressId },
                { ContentItemAssociationEventOperation.Modifying, ContentItemAssociationModifyingEventAddressId },
                { ContentItemAssociationEventOperation.RemovingById, ContentItemAssociationRemovingByIdEventAddressId },
                {
                    ContentItemAssociationEventOperation.HardRemovingById,
                    ContentItemAssociationHardRemovingByIdEventAddressId
                },
                {
                    ContentItemAssociationEventOperation.RetrievingById,
                    ContentItemAssociationRetrievingByIdEventAddressId
                },
                { ContentItemAssociationEventOperation.Added, ContentItemAssociationAddedEventAddressId },
                { ContentItemAssociationEventOperation.Modified, ContentItemAssociationModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals by
                // the composed event name ("ContentItemAssociationHardRemoved" vs
                // "ContentItemAssociationRemoved").
                { ContentItemAssociationEventOperation.Removed, ContentItemAssociationRemovedEventAddressId },
                { ContentItemAssociationEventOperation.HardRemoved, ContentItemAssociationRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemAssociationEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemAssociationAddingEventAddressId, "ContentItemAssociation-Adding" },
                { ContentItemAssociationModifyingEventAddressId, "ContentItemAssociation-Modifying" },
                { ContentItemAssociationRemovingByIdEventAddressId, "ContentItemAssociation-RemovingById" },
                {
                    ContentItemAssociationHardRemovingByIdEventAddressId,
                    "ContentItemAssociation-HardRemovingById"
                },
                { ContentItemAssociationRetrievingByIdEventAddressId, "ContentItemAssociation-RetrievingById" },
                { ContentItemAssociationAddedEventAddressId, "ContentItemAssociation-Added" },
                { ContentItemAssociationModifiedEventAddressId, "ContentItemAssociation-Modified" },
                { ContentItemAssociationRemovedEventAddressId, "ContentItemAssociation-Removed" }
            };

        public static readonly Guid ContentItemAssociationOnAddingContentItemAssociationSubscriptionId =
            new Guid("019f8170-a642-7cec-bc2e-da65a18d6c88");

        public const string ContentItemAssociationOnAddingContentItemAssociationSubscriptionName =
            "ContentItemAssociationService.OnAddingContentItemAssociation";
        public static readonly Guid ContentItemAssociationOnModifyingContentItemAssociationSubscriptionId =
            new Guid("019f8170-a642-78f9-9aa0-1e0bb425e800");

        public const string ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName =
            "ContentItemAssociationService.OnModifyingContentItemAssociation";
        public static readonly Guid ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionId =
            new Guid("019f8170-a642-76fd-8543-bdf4222337e8");

        public const string ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName =
            "ContentItemAssociationService.OnRemovingContentItemAssociationById";
        public static readonly Guid ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionId =
            new Guid("019f855d-4517-7ee8-82fc-fca27d45609e");

        public const string ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName =
            "ContentItemAssociationService.OnHardRemovingContentItemAssociationById";

        public static readonly Guid ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionId =
            new Guid("019f8170-a642-7af2-a066-e99b09e67a3e");

        public const string ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionName =
            "ContentItemAssociationService.OnRetrievingContentItemAssociationById";
    }
}
