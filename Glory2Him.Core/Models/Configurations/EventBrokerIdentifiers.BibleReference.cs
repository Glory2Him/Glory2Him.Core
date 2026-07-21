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
        public static readonly Guid BibleReferenceAddingEventAddressId =
            new Guid("019f814e-89c1-7032-b815-7cc5a4706422");

        public static readonly Guid BibleReferenceModifyingEventAddressId =
            new Guid("019f814e-89c1-782f-a5d9-2e78df814247");

        public static readonly Guid BibleReferenceRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7932-8191-791824d55965");

        public static readonly Guid BibleReferenceRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7598-b254-42bf98366d02");

        public static readonly Guid BibleReferenceAddedEventAddressId =
            new Guid("019f814e-89c1-7cb2-b40b-40d95d0cc76d");

        public static readonly Guid BibleReferenceModifiedEventAddressId =
            new Guid("019f814e-89c1-7bfb-82e0-01b077760ea7");

        public static readonly Guid BibleReferenceRemovedEventAddressId =
            new Guid("019f814e-89c1-70c1-9991-6d52f85829d1");

        internal static readonly IReadOnlyDictionary<BibleReferenceEventOperation, Guid>
            BibleReferenceEventAddressIds = new Dictionary<BibleReferenceEventOperation, Guid>
            {
                { BibleReferenceEventOperation.Adding, BibleReferenceAddingEventAddressId },
                { BibleReferenceEventOperation.Modifying, BibleReferenceModifyingEventAddressId },
                { BibleReferenceEventOperation.RemovingById, BibleReferenceRemovingByIdEventAddressId },
                { BibleReferenceEventOperation.RetrievingById, BibleReferenceRetrievingByIdEventAddressId },
                { BibleReferenceEventOperation.Added, BibleReferenceAddedEventAddressId },
                { BibleReferenceEventOperation.Modified, BibleReferenceModifiedEventAddressId },
                { BibleReferenceEventOperation.Removed, BibleReferenceRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> BibleReferenceEventAddresses =
            new Dictionary<Guid, string>
            {
                { BibleReferenceAddingEventAddressId, "BibleReference-Adding" },
                { BibleReferenceModifyingEventAddressId, "BibleReference-Modifying" },
                { BibleReferenceRemovingByIdEventAddressId, "BibleReference-RemovingById" },
                { BibleReferenceRetrievingByIdEventAddressId, "BibleReference-RetrievingById" },
                { BibleReferenceAddedEventAddressId, "BibleReference-Added" },
                { BibleReferenceModifiedEventAddressId, "BibleReference-Modified" },
                { BibleReferenceRemovedEventAddressId, "BibleReference-Removed" }
            };

        public static readonly Guid BibleReferenceOnAddingBibleReferenceSubscriptionId =
            new Guid("019f8170-a642-71cf-969c-f82e96fc5a88");

        public const string BibleReferenceOnAddingBibleReferenceSubscriptionName =
            "BibleReferenceService.OnAddingBibleReference";
        public static readonly Guid BibleReferenceOnModifyingBibleReferenceSubscriptionId =
            new Guid("019f8170-a642-7804-9908-9d6486c6ccbc");

        public const string BibleReferenceOnModifyingBibleReferenceSubscriptionName =
            "BibleReferenceService.OnModifyingBibleReference";
        public static readonly Guid BibleReferenceOnRemovingBibleReferenceByIdSubscriptionId =
            new Guid("019f8170-a642-73d4-b880-968a6b8a08ca");

        public const string BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName =
            "BibleReferenceService.OnRemovingBibleReferenceById";
        public static readonly Guid BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionId =
            new Guid("019f8170-a642-772f-b896-2233aca4eb26");

        public const string BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionName =
            "BibleReferenceService.OnRetrievingBibleReferenceById";
    }
}
