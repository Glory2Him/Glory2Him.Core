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
using Glory2Him.Core.Models.Events.Orchestrations;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        public static readonly Guid ContentItemOrchestrationAddingEventAddressId =
            new Guid("019fab26-8096-7793-9db4-b88aa480fa6e");

        public static readonly Guid ContentItemOrchestrationModifyingEventAddressId =
            new Guid("019fb20f-c2ea-78ee-a781-821e6c7ec657");

        public static readonly Guid ContentItemOrchestrationRemovingByIdEventAddressId =
            new Guid("019fba3f-b27d-736c-8984-3c900911dfef");

        public static readonly Guid ContentItemOrchestrationAddedEventAddressId =
            new Guid("019fba7b-a32e-7f00-8a60-bca984bf36bd");

        public static readonly Guid ContentItemOrchestrationModifiedEventAddressId =
            new Guid("019fba7b-a339-7ede-9512-436bbca6932b");

        public static readonly Guid ContentItemOrchestrationRemovedEventAddressId =
            new Guid("019fba7b-a344-7ede-b512-436bbca6932b");

        internal static readonly IReadOnlyDictionary<ContentItemOrchestrationEventOperation, Guid>
            ContentItemOrchestrationEventAddressIds = new Dictionary<ContentItemOrchestrationEventOperation, Guid>
            {
                {
                    ContentItemOrchestrationEventOperation.Adding,
                    ContentItemOrchestrationAddingEventAddressId
                },
                {
                    ContentItemOrchestrationEventOperation.Modifying,
                    ContentItemOrchestrationModifyingEventAddressId
                },
                {
                    ContentItemOrchestrationEventOperation.RemovingById,
                    ContentItemOrchestrationRemovingByIdEventAddressId
                },
                {
                    ContentItemOrchestrationEventOperation.Added,
                    ContentItemOrchestrationAddedEventAddressId
                },
                {
                    ContentItemOrchestrationEventOperation.Modified,
                    ContentItemOrchestrationModifiedEventAddressId
                },
                {
                    ContentItemOrchestrationEventOperation.Removed,
                    ContentItemOrchestrationRemovedEventAddressId
                }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemOrchestrationEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemOrchestrationAddingEventAddressId, "ContentItemOrchestration-Adding" },
                { ContentItemOrchestrationModifyingEventAddressId, "ContentItemOrchestration-Modifying" },
                { ContentItemOrchestrationRemovingByIdEventAddressId, "ContentItemOrchestration-RemovingById" },
                { ContentItemOrchestrationAddedEventAddressId, "ContentItemOrchestration-Added" },
                { ContentItemOrchestrationModifiedEventAddressId, "ContentItemOrchestration-Modified" },
                { ContentItemOrchestrationRemovedEventAddressId, "ContentItemOrchestration-Removed" }
            };

        public static readonly Guid ContentItemOrchestrationOnAddingContentItemSubscriptionId =
            new Guid("019fab26-8096-7007-866e-805ae4cfab7f");

        public const string ContentItemOrchestrationOnAddingContentItemSubscriptionName =
            "ContentItemOrchestrationService.OnAddingContentItem";

        public static readonly Guid ContentItemOrchestrationOnModifyingContentItemSubscriptionId =
            new Guid("019fb20f-c3f7-776e-a0f9-84f99984fbb0");

        public const string ContentItemOrchestrationOnModifyingContentItemSubscriptionName =
            "ContentItemOrchestrationService.OnModifyingContentItem";

        public static readonly Guid ContentItemOrchestrationOnRemovingContentItemByIdSubscriptionId =
            new Guid("019fba3f-b284-734a-a436-c35341f73c5e");

        public const string ContentItemOrchestrationOnRemovingContentItemByIdSubscriptionName =
            "ContentItemOrchestrationService.OnRemovingContentItemById";
    }
}
