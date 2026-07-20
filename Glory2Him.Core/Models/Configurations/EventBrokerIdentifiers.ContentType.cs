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
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Models.Configurations
{
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid ContentTypeAddingEventAddressId =
            new Guid("019f814e-89c1-7be0-8910-b1dfa2f45bae");

        public static readonly Guid ContentTypeModifyingEventAddressId =
            new Guid("019f814e-89c1-70a6-a5b4-e6cce066c5b6");

        public static readonly Guid ContentTypeRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7bbe-83b0-c311d0d3c4e2");

        public static readonly Guid ContentTypeRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-72c3-b9e0-ceb41f647ac3");

        public static readonly Guid ContentTypeAddedEventAddressId =
            new Guid("019f814e-89c1-7675-92b1-58df00a8f390");

        public static readonly Guid ContentTypeModifiedEventAddressId =
            new Guid("019f814e-89c1-7bd6-beb6-b9481ede7455");

        public static readonly Guid ContentTypeRemovedEventAddressId =
            new Guid("019f814e-89c1-7840-a627-950aedc612e7");

        internal static readonly IReadOnlyDictionary<ContentTypeEventOperation, Guid>
            ContentTypeEventAddressIds = new Dictionary<ContentTypeEventOperation, Guid>
            {
                { ContentTypeEventOperation.Adding, ContentTypeAddingEventAddressId },
                { ContentTypeEventOperation.Modifying, ContentTypeModifyingEventAddressId },
                { ContentTypeEventOperation.RemovingById, ContentTypeRemovingByIdEventAddressId },
                { ContentTypeEventOperation.RetrievingById, ContentTypeRetrievingByIdEventAddressId },
                { ContentTypeEventOperation.Added, ContentTypeAddedEventAddressId },
                { ContentTypeEventOperation.Modified, ContentTypeModifiedEventAddressId },
                { ContentTypeEventOperation.Removed, ContentTypeRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentTypeEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentTypeAddingEventAddressId, "ContentType-Adding" },
                { ContentTypeModifyingEventAddressId, "ContentType-Modifying" },
                { ContentTypeRemovingByIdEventAddressId, "ContentType-RemovingById" },
                { ContentTypeRetrievingByIdEventAddressId, "ContentType-RetrievingById" },
                { ContentTypeAddedEventAddressId, "ContentType-Added" },
                { ContentTypeModifiedEventAddressId, "ContentType-Modified" },
                { ContentTypeRemovedEventAddressId, "ContentType-Removed" }
            };

        public static readonly Guid ContentTypeOnAddingContentTypeSubscriptionId =
            new Guid("019f8150-13e6-7fd4-82fa-72eb3d9185b5");

        public const string ContentTypeOnAddingContentTypeSubscriptionName =
            "ContentTypeService.OnAddingContentType";
        public static readonly Guid ContentTypeOnModifyingContentTypeSubscriptionId =
            new Guid("019f8150-13e6-7634-bc07-bbfaee2dc778");

        public const string ContentTypeOnModifyingContentTypeSubscriptionName =
            "ContentTypeService.OnModifyingContentType";
        public static readonly Guid ContentTypeOnRemovingContentTypeByIdSubscriptionId =
            new Guid("019f8150-13e6-7e6a-b550-ac7e692039e6");

        public const string ContentTypeOnRemovingContentTypeByIdSubscriptionName =
            "ContentTypeService.OnRemovingContentTypeById";
        public static readonly Guid ContentTypeOnRetrievingContentTypeByIdSubscriptionId =
            new Guid("019f8150-13e6-717a-98ae-be13a25bbf50");

        public const string ContentTypeOnRetrievingContentTypeByIdSubscriptionName =
            "ContentTypeService.OnRetrievingContentTypeById";
    }
}
