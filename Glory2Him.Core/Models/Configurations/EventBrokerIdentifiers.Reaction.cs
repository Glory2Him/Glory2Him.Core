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
        public static readonly Guid ReactionAddingEventAddressId =
            new Guid("019f814e-89c1-7c63-bc4e-eea12ab72bab");

        public static readonly Guid ReactionModifyingEventAddressId =
            new Guid("019f814e-89c1-7ccc-84e5-551b60fd5002");

        public static readonly Guid ReactionRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-76ca-b0db-51e854ad03e7");

        public static readonly Guid ReactionRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-79a2-94de-bd92b5249148");

        public static readonly Guid ReactionAddedEventAddressId =
            new Guid("019f814e-89c1-7991-82a1-978857130455");

        public static readonly Guid ReactionModifiedEventAddressId =
            new Guid("019f814e-89c1-7d65-b134-8979248e0766");

        public static readonly Guid ReactionRemovedEventAddressId =
            new Guid("019f814e-89c1-7f30-9317-3385e9c5f6ae");

        internal static readonly IReadOnlyDictionary<ReactionEventOperation, Guid> ReactionEventAddressIds =
            new Dictionary<ReactionEventOperation, Guid>
            {
                { ReactionEventOperation.Adding, ReactionAddingEventAddressId },
                { ReactionEventOperation.Modifying, ReactionModifyingEventAddressId },
                { ReactionEventOperation.RemovingById, ReactionRemovingByIdEventAddressId },
                { ReactionEventOperation.RetrievingById, ReactionRetrievingByIdEventAddressId },
                { ReactionEventOperation.Added, ReactionAddedEventAddressId },
                { ReactionEventOperation.Modified, ReactionModifiedEventAddressId },
                { ReactionEventOperation.Removed, ReactionRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ReactionEventAddresses =
            new Dictionary<Guid, string>
            {
                { ReactionAddingEventAddressId, "Reaction-Adding" },
                { ReactionModifyingEventAddressId, "Reaction-Modifying" },
                { ReactionRemovingByIdEventAddressId, "Reaction-RemovingById" },
                { ReactionRetrievingByIdEventAddressId, "Reaction-RetrievingById" },
                { ReactionAddedEventAddressId, "Reaction-Added" },
                { ReactionModifiedEventAddressId, "Reaction-Modified" },
                { ReactionRemovedEventAddressId, "Reaction-Removed" }
            };

        public static readonly Guid ReactionOnAddingReactionSubscriptionId =
            new Guid("019f8170-a642-7c77-884d-b5c2caa3be0b");

        public const string ReactionOnAddingReactionSubscriptionName =
            "ReactionService.OnAddingReaction";
        public static readonly Guid ReactionOnModifyingReactionSubscriptionId =
            new Guid("019f8170-a642-77af-97ec-d0e5d0d77501");

        public const string ReactionOnModifyingReactionSubscriptionName =
            "ReactionService.OnModifyingReaction";
        public static readonly Guid ReactionOnRemovingReactionByIdSubscriptionId =
            new Guid("019f8170-a642-750d-83eb-23e596af329f");

        public const string ReactionOnRemovingReactionByIdSubscriptionName =
            "ReactionService.OnRemovingReactionById";
        public static readonly Guid ReactionOnRetrievingReactionByIdSubscriptionId =
            new Guid("019f8170-a642-774a-adab-4abec905d9ea");

        public const string ReactionOnRetrievingReactionByIdSubscriptionName =
            "ReactionService.OnRetrievingReactionById";
    }
}
