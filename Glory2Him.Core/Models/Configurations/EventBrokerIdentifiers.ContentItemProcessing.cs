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
using Glory2Him.Core.Models.Events.Processings;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        public static readonly Guid ContentItemProcessingAddingEventAddressId =
            new Guid("019fab26-8096-7793-9db4-b88aa480fa6e");

        public static readonly Guid ContentItemProcessingModifyingEventAddressId =
            new Guid("019fb20f-c2ea-78ee-a781-821e6c7ec657");

        public static readonly Guid ContentItemProcessingRemovingByIdEventAddressId =
            new Guid("019fba3f-b27d-736c-8984-3c900911dfef");

        public static readonly Guid ContentItemProcessingRetrievingByIdEventAddressId =
            new Guid("019fbf0b-835d-71c2-8b2c-6ec3cefde90f");

        public static readonly Guid ContentItemProcessingAddedEventAddressId =
            new Guid("019fba7b-a32e-7f00-8a60-bca984bf36bd");

        public static readonly Guid ContentItemProcessingModifiedEventAddressId =
            new Guid("019fba7b-a339-7ede-9512-436bbca6932b");

        public static readonly Guid ContentItemProcessingRemovedEventAddressId =
            new Guid("019fba7b-a344-7ede-b512-436bbca6932b");

        // The publication swap's request and completion addresses. A Versioned entity is
        // approved through the PROCESSING tier, because granting approval also has to
        // clear the group's published slot and only this layer can order the two writes
        // (§12.4.1 rule 10, §9.7.7 rule 7).
        public static readonly Guid ContentItemProcessingApprovingEventAddressId =
            new Guid("01a01034-ea12-7689-be79-4f2d9a6ac2c0");

        public static readonly Guid ContentItemProcessingApprovedEventAddressId =
            new Guid("01a01034-fb23-779a-cf8a-503eab7bd3d1");

        internal static readonly IReadOnlyDictionary<ContentItemProcessingEventOperation, Guid>
            ContentItemProcessingEventAddressIds = new Dictionary<ContentItemProcessingEventOperation, Guid>
            {
                {
                    ContentItemProcessingEventOperation.Adding,
                    ContentItemProcessingAddingEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Modifying,
                    ContentItemProcessingModifyingEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.RemovingById,
                    ContentItemProcessingRemovingByIdEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.RetrievingById,
                    ContentItemProcessingRetrievingByIdEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Added,
                    ContentItemProcessingAddedEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Modified,
                    ContentItemProcessingModifiedEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Removed,
                    ContentItemProcessingRemovedEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Approving,
                    ContentItemProcessingApprovingEventAddressId
                },
                {
                    ContentItemProcessingEventOperation.Approved,
                    ContentItemProcessingApprovedEventAddressId
                }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemProcessingEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemProcessingAddingEventAddressId, "ContentItemProcessing-Adding" },
                { ContentItemProcessingModifyingEventAddressId, "ContentItemProcessing-Modifying" },
                { ContentItemProcessingRemovingByIdEventAddressId, "ContentItemProcessing-RemovingById" },
                { ContentItemProcessingRetrievingByIdEventAddressId, "ContentItemProcessing-RetrievingById" },
                { ContentItemProcessingAddedEventAddressId, "ContentItemProcessing-Added" },
                { ContentItemProcessingModifiedEventAddressId, "ContentItemProcessing-Modified" },
                { ContentItemProcessingRemovedEventAddressId, "ContentItemProcessing-Removed" },
                { ContentItemProcessingApprovingEventAddressId, "ContentItemProcessing-Approving" },
                { ContentItemProcessingApprovedEventAddressId, "ContentItemProcessing-Approved" }
            };

        public static readonly Guid ContentItemProcessingOnApprovingContentItemSubscriptionId =
            new Guid("019ff41f-4b3a-7d77-ce60-9b8da4f701e6");

        public const string ContentItemProcessingOnApprovingContentItemSubscriptionName =
            "ContentItemProcessing.OnApprovingContentItem";

        public static readonly Guid ContentItemProcessingOnAddingContentItemSubscriptionId =
            new Guid("019fab26-8096-7007-866e-805ae4cfab7f");

        public const string ContentItemProcessingOnAddingContentItemSubscriptionName =
            "ContentItemProcessingService.OnAddingContentItem";

        public static readonly Guid ContentItemProcessingOnModifyingContentItemSubscriptionId =
            new Guid("019fb20f-c3f7-776e-a0f9-84f99984fbb0");

        public const string ContentItemProcessingOnModifyingContentItemSubscriptionName =
            "ContentItemProcessingService.OnModifyingContentItem";

        public static readonly Guid ContentItemProcessingOnRemovingContentItemByIdSubscriptionId =
            new Guid("019fba3f-b284-734a-a436-c35341f73c5e");

        public const string ContentItemProcessingOnRemovingContentItemByIdSubscriptionName =
            "ContentItemProcessingService.OnRemovingContentItemById";

        public static readonly Guid ContentItemProcessingOnRetrievingContentItemByIdSubscriptionId =
            new Guid("019fbf0b-83eb-7488-8bb4-9020462b997b");

        public const string ContentItemProcessingOnRetrievingContentItemByIdSubscriptionName =
            "ContentItemProcessingService.OnRetrievingContentItemById";
    }
}
