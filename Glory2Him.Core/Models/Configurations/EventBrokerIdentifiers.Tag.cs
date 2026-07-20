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
        public static readonly Guid TagAddingEventAddressId =
            new Guid("019f814e-89c1-70ce-98f3-289e2e5d65fd");

        public static readonly Guid TagModifyingEventAddressId =
            new Guid("019f814e-89c1-7a99-931c-9692dee45bf9");

        public static readonly Guid TagRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7cba-91cf-2019447d01ed");

        public static readonly Guid TagRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7bbf-9eda-e1d0826112fe");

        public static readonly Guid TagAddedEventAddressId =
            new Guid("019f814e-89c1-7f9b-9a5f-16c3f8fde976");

        public static readonly Guid TagModifiedEventAddressId =
            new Guid("019f814e-89c1-7da8-ab9c-fe74c5f28dc7");

        public static readonly Guid TagRemovedEventAddressId =
            new Guid("019f814e-89c1-79ef-973f-e3a8b3e12b93");

        internal static readonly IReadOnlyDictionary<TagEventOperation, Guid> TagEventAddressIds =
            new Dictionary<TagEventOperation, Guid>
            {
                { TagEventOperation.Adding, TagAddingEventAddressId },
                { TagEventOperation.Modifying, TagModifyingEventAddressId },
                { TagEventOperation.RemovingById, TagRemovingByIdEventAddressId },
                { TagEventOperation.RetrievingById, TagRetrievingByIdEventAddressId },
                { TagEventOperation.Added, TagAddedEventAddressId },
                { TagEventOperation.Modified, TagModifiedEventAddressId },
                { TagEventOperation.Removed, TagRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> TagEventAddresses =
            new Dictionary<Guid, string>
            {
                { TagAddingEventAddressId, "Tag-Adding" },
                { TagModifyingEventAddressId, "Tag-Modifying" },
                { TagRemovingByIdEventAddressId, "Tag-RemovingById" },
                { TagRetrievingByIdEventAddressId, "Tag-RetrievingById" },
                { TagAddedEventAddressId, "Tag-Added" },
                { TagModifiedEventAddressId, "Tag-Modified" },
                { TagRemovedEventAddressId, "Tag-Removed" }
            };

        public static readonly Guid TagOnAddingTagSubscriptionId =
            new Guid("019f8170-a642-75ed-95fc-8f83fa756e84");

        public const string TagOnAddingTagSubscriptionName =
            "TagService.OnAddingTag";
        public static readonly Guid TagOnModifyingTagSubscriptionId =
            new Guid("019f8170-a642-7f5c-bd7f-5dd85f045b17");

        public const string TagOnModifyingTagSubscriptionName =
            "TagService.OnModifyingTag";
        public static readonly Guid TagOnRemovingTagByIdSubscriptionId =
            new Guid("019f8170-a642-7ffc-9761-d85d7b68aaad");

        public const string TagOnRemovingTagByIdSubscriptionName =
            "TagService.OnRemovingTagById";
        public static readonly Guid TagOnRetrievingTagByIdSubscriptionId =
            new Guid("019f8170-a642-71e1-9a51-0a229986ef09");

        public const string TagOnRetrievingTagByIdSubscriptionName =
            "TagService.OnRetrievingTagById";
    }
}
