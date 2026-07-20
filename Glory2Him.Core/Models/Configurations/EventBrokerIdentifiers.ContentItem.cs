// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Models.Configurations
{
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid ContentItemAddingEventAddressId =
            new Guid("019f814e-89c1-7f83-829f-f14cdc5e0ee5");

        public static readonly Guid ContentItemModifyingEventAddressId =
            new Guid("019f814e-89c1-7366-94b4-1a5b853c833c");

        public static readonly Guid ContentItemRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7767-b51a-74fdd4f774e2");

        public static readonly Guid ContentItemRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7124-b2cb-8c84e2d62595");

        public static readonly Guid ContentItemAddedEventAddressId =
            new Guid("019f814e-89c1-77b3-8ab9-c58108075f03");

        public static readonly Guid ContentItemModifiedEventAddressId =
            new Guid("019f814e-89c1-77ea-83bc-acbab6e6c590");

        public static readonly Guid ContentItemRemovedEventAddressId =
            new Guid("019f814e-89c1-7f9e-977e-873e2c2b36e0");

        internal static readonly IReadOnlyDictionary<ContentItemEventOperation, Guid> ContentItemEventAddressIds =
            new Dictionary<ContentItemEventOperation, Guid>
            {
                { ContentItemEventOperation.Adding, ContentItemAddingEventAddressId },
                { ContentItemEventOperation.Modifying, ContentItemModifyingEventAddressId },
                { ContentItemEventOperation.RemovingById, ContentItemRemovingByIdEventAddressId },
                { ContentItemEventOperation.RetrievingById, ContentItemRetrievingByIdEventAddressId },
                { ContentItemEventOperation.Added, ContentItemAddedEventAddressId },
                { ContentItemEventOperation.Modified, ContentItemModifiedEventAddressId },
                { ContentItemEventOperation.Removed, ContentItemRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemAddingEventAddressId, "ContentItem-Adding" },
                { ContentItemModifyingEventAddressId, "ContentItem-Modifying" },
                { ContentItemRemovingByIdEventAddressId, "ContentItem-RemovingById" },
                { ContentItemRetrievingByIdEventAddressId, "ContentItem-RetrievingById" },
                { ContentItemAddedEventAddressId, "ContentItem-Added" },
                { ContentItemModifiedEventAddressId, "ContentItem-Modified" },
                { ContentItemRemovedEventAddressId, "ContentItem-Removed" }
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
        public static readonly Guid ContentItemOnRetrievingContentItemByIdSubscriptionId =
            new Guid("019f8150-13e6-7b66-9e4b-a145c6ec98e3");

        public const string ContentItemOnRetrievingContentItemByIdSubscriptionName =
            "ContentItemService.OnRetrievingContentItemById";
    }
}
