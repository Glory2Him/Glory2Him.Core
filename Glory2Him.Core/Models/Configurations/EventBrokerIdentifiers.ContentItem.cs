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
        public static readonly Guid ContentItemAddingEventAddressId =
            new Guid("019f814e-89c1-7f83-829f-f14cdc5e0ee5");

        public static readonly Guid ContentItemModifyingEventAddressId =
            new Guid("019f814e-89c1-7366-94b4-1a5b853c833c");

        public static readonly Guid ContentItemRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7767-b51a-74fdd4f774e2");

        public static readonly Guid ContentItemHardRemovingByIdEventAddressId =
            new Guid("019f8152-4b7d-7c56-a3e8-91d47b2f6a05");

        public static readonly Guid ContentItemRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7124-b2cb-8c84e2d62595");

        public static readonly Guid ContentItemAddedEventAddressId =
            new Guid("019f814e-89c1-77b3-8ab9-c58108075f03");

        public static readonly Guid ContentItemModifiedEventAddressId =
            new Guid("019f814e-89c1-77ea-83bc-acbab6e6c590");

        public static readonly Guid ContentItemRemovedEventAddressId =
            new Guid("019f814e-89c1-7f9e-977e-873e2c2b36e0");

        public static readonly Guid ContentItemSubmittingEventAddressId =
            new Guid("753fe6cb-6c0f-4a9d-8b0c-9584c9a257af");

        public static readonly Guid ContentItemApprovingEventAddressId =
            new Guid("528e08fb-b2da-4c6b-95a2-2d3737dc0673");

        public static readonly Guid ContentItemSubmittedEventAddressId =
            new Guid("248329d6-6809-46e8-970d-d851fbd43ee3");

        public static readonly Guid ContentItemApprovedEventAddressId =
            new Guid("f7297161-3e6f-46d3-9cba-9e08d48c5d2c");

        public static readonly Guid ContentItemRejectedEventAddressId =
            new Guid("c9c78e35-b357-42dd-aeab-697bc05a8b7a");

        internal static readonly IReadOnlyDictionary<ContentItemEventOperation, Guid>
            ContentItemEventAddressIds = new Dictionary<ContentItemEventOperation, Guid>
            {
                { ContentItemEventOperation.Adding, ContentItemAddingEventAddressId },
                { ContentItemEventOperation.Modifying, ContentItemModifyingEventAddressId },
                { ContentItemEventOperation.RemovingById, ContentItemRemovingByIdEventAddressId },
                { ContentItemEventOperation.HardRemovingById, ContentItemHardRemovingByIdEventAddressId },
                { ContentItemEventOperation.RetrievingById, ContentItemRetrievingByIdEventAddressId },
                { ContentItemEventOperation.Submitting, ContentItemSubmittingEventAddressId },
                { ContentItemEventOperation.Approving, ContentItemApprovingEventAddressId },
                { ContentItemEventOperation.Added, ContentItemAddedEventAddressId },
                { ContentItemEventOperation.Modified, ContentItemModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("ContentItemHardRemoved" vs "ContentItemRemoved").
                { ContentItemEventOperation.Removed, ContentItemRemovedEventAddressId },
                { ContentItemEventOperation.HardRemoved, ContentItemRemovedEventAddressId },

                { ContentItemEventOperation.Submitted, ContentItemSubmittedEventAddressId },
                { ContentItemEventOperation.Approved, ContentItemApprovedEventAddressId },
                { ContentItemEventOperation.Rejected, ContentItemRejectedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemAddingEventAddressId, "ContentItem-Adding" },
                { ContentItemModifyingEventAddressId, "ContentItem-Modifying" },
                { ContentItemRemovingByIdEventAddressId, "ContentItem-RemovingById" },
                { ContentItemHardRemovingByIdEventAddressId, "ContentItem-HardRemovingById" },
                { ContentItemRetrievingByIdEventAddressId, "ContentItem-RetrievingById" },
                { ContentItemSubmittingEventAddressId, "ContentItem-Submitting" },
                { ContentItemApprovingEventAddressId, "ContentItem-Approving" },
                { ContentItemAddedEventAddressId, "ContentItem-Added" },
                { ContentItemModifiedEventAddressId, "ContentItem-Modified" },
                { ContentItemRemovedEventAddressId, "ContentItem-Removed" },
                { ContentItemSubmittedEventAddressId, "ContentItem-Submitted" },
                { ContentItemApprovedEventAddressId, "ContentItem-Approved" },
                { ContentItemRejectedEventAddressId, "ContentItem-Rejected" }
            };

        public static readonly Guid ContentItemOnAddingContentItemSubscriptionId =
            new Guid("019f8150-13e6-7411-9c35-38d08c0cfdb1");

        public const string ContentItemOnAddingContentItemSubscriptionName =
            "ContentItemService.OnAddingContentItem";
        public static readonly Guid ContentItemOnModifyingContentItemSubscriptionId =
            new Guid("019f8150-13e6-7d73-8b6f-dba995c7a13d");

        public const string ContentItemOnModifyingContentItemSubscriptionName =
            "ContentItemService.OnModifyingContentItem";
        public static readonly Guid ContentItemOnRemovingContentItemByIdSubscriptionId =
            new Guid("019f8150-13e6-72a0-a41e-a2011dcd1eef");

        public const string ContentItemOnRemovingContentItemByIdSubscriptionName =
            "ContentItemService.OnRemovingContentItemById";
        public static readonly Guid ContentItemOnHardRemovingContentItemByIdSubscriptionId =
            new Guid("019f8152-4b7d-71c4-b7d2-8e05a39f61c8");

        public const string ContentItemOnHardRemovingContentItemByIdSubscriptionName =
            "ContentItemService.OnHardRemovingContentItemById";

        public static readonly Guid ContentItemOnRetrievingContentItemByIdSubscriptionId =
            new Guid("019f8150-13e6-7b66-9e4b-a145c6ec98e3");

        public const string ContentItemOnRetrievingContentItemByIdSubscriptionName =
            "ContentItemService.OnRetrievingContentItemById";

        public static readonly Guid ContentItemOnSubmittingContentItemSubscriptionId =
            new Guid("ab43cac6-55c6-4224-aeff-1aeb0666b852");

        public const string ContentItemOnSubmittingContentItemSubscriptionName =
            "ContentItemService.OnSubmittingContentItem";

        public static readonly Guid ContentItemOnApprovingContentItemSubscriptionId =
            new Guid("f7621b20-132e-498d-b095-b5f9468d8f3a");

        public const string ContentItemOnApprovingContentItemSubscriptionName =
            "ContentItemService.OnApprovingContentItem";
    }
}
