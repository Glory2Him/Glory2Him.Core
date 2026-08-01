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
        public static readonly Guid ContentItemSettingAddingEventAddressId =
            new Guid("019f814e-89c1-733f-a649-23a14381d365");

        public static readonly Guid ContentItemSettingModifyingEventAddressId =
            new Guid("019f814e-89c1-775b-b5ee-d4ae0d92f8e4");

        public static readonly Guid ContentItemSettingRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7fc0-a097-a348f815b3f7");

        public static readonly Guid ContentItemSettingHardRemovingByIdEventAddressId =
            new Guid("019f855d-4518-75de-8a2a-91ac37d1191f");

        public static readonly Guid ContentItemSettingRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7bda-a7b2-5ccffee89137");

        public static readonly Guid ContentItemSettingAddedEventAddressId =
            new Guid("019f814e-89c1-7234-a6d7-3808998db3c8");

        public static readonly Guid ContentItemSettingModifiedEventAddressId =
            new Guid("019f814e-89c1-793b-9983-c8d576dfe18d");

        public static readonly Guid ContentItemSettingRemovedEventAddressId =
            new Guid("019f814e-89c1-7ca7-8fb8-25f6910ceffe");

        internal static readonly IReadOnlyDictionary<ContentItemSettingEventOperation, Guid>
            ContentItemSettingEventAddressIds = new Dictionary<ContentItemSettingEventOperation, Guid>
            {
                { ContentItemSettingEventOperation.Adding, ContentItemSettingAddingEventAddressId },
                { ContentItemSettingEventOperation.Modifying, ContentItemSettingModifyingEventAddressId },
                { ContentItemSettingEventOperation.RemovingById, ContentItemSettingRemovingByIdEventAddressId },

                { ContentItemSettingEventOperation.HardRemovingById,
                    ContentItemSettingHardRemovingByIdEventAddressId },

                { ContentItemSettingEventOperation.RetrievingById, ContentItemSettingRetrievingByIdEventAddressId },
                { ContentItemSettingEventOperation.Added, ContentItemSettingAddedEventAddressId },
                { ContentItemSettingEventOperation.Modified, ContentItemSettingModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals by
                // the composed event name ("ContentItemSettingHardRemoved" vs
                // "ContentItemSettingRemoved").
                { ContentItemSettingEventOperation.Removed, ContentItemSettingRemovedEventAddressId },
                { ContentItemSettingEventOperation.HardRemoved, ContentItemSettingRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemSettingEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemSettingAddingEventAddressId, "ContentItemSetting-Adding" },
                { ContentItemSettingModifyingEventAddressId, "ContentItemSetting-Modifying" },
                { ContentItemSettingRemovingByIdEventAddressId, "ContentItemSetting-RemovingById" },

                { ContentItemSettingHardRemovingByIdEventAddressId,
                    "ContentItemSetting-HardRemovingById" },

                { ContentItemSettingRetrievingByIdEventAddressId, "ContentItemSetting-RetrievingById" },
                { ContentItemSettingAddedEventAddressId, "ContentItemSetting-Added" },
                { ContentItemSettingModifiedEventAddressId, "ContentItemSetting-Modified" },
                { ContentItemSettingRemovedEventAddressId, "ContentItemSetting-Removed" }
            };

        public static readonly Guid ContentItemSettingOnAddingContentItemSettingSubscriptionId =
            new Guid("019f8170-a642-7920-a896-c0840ea4bb65");

        public const string ContentItemSettingOnAddingContentItemSettingSubscriptionName =
            "ContentItemSettingService.OnAddingContentItemSetting";
        public static readonly Guid ContentItemSettingOnModifyingContentItemSettingSubscriptionId =
            new Guid("019f8170-a642-72ba-85d8-8e5d3e107c5e");

        public const string ContentItemSettingOnModifyingContentItemSettingSubscriptionName =
            "ContentItemSettingService.OnModifyingContentItemSetting";
        public static readonly Guid ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionId =
            new Guid("019f8170-a642-71c9-b96f-938d450db761");

        public const string ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName =
            "ContentItemSettingService.OnRemovingContentItemSettingById";
        public static readonly Guid ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionId =
            new Guid("019f855d-4519-7971-878b-cddbc1df9f38");

        public const string ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName =
            "ContentItemSettingService.OnHardRemovingContentItemSettingById";

        public static readonly Guid ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionId =
            new Guid("019f8170-a642-7883-b8a4-be180f50b89b");

        public const string ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionName =
            "ContentItemSettingService.OnRetrievingContentItemSettingById";
    }
}
