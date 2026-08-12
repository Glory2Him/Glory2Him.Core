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
        public static readonly Guid BibleReferenceAddingEventAddressId =
            new Guid("019f814e-89c1-7032-b815-7cc5a4706422");

        public static readonly Guid BibleReferenceModifyingEventAddressId =
            new Guid("019f814e-89c1-782f-a5d9-2e78df814247");

        public static readonly Guid BibleReferenceRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7932-8191-791824d55965");

        public static readonly Guid BibleReferenceHardRemovingByIdEventAddressId =
            new Guid("019f855d-4512-7511-89c0-8979e152a40e");

        public static readonly Guid BibleReferenceRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7598-b254-42bf98366d02");

        public static readonly Guid BibleReferenceAddedEventAddressId =
            new Guid("019f814e-89c1-7cb2-b40b-40d95d0cc76d");

        public static readonly Guid BibleReferenceModifiedEventAddressId =
            new Guid("019f814e-89c1-7bfb-82e0-01b077760ea7");

        public static readonly Guid BibleReferenceRemovedEventAddressId =
            new Guid("019f814e-89c1-70c1-9991-6d52f85829d1");

        public static readonly Guid BibleReferenceSubmittingEventAddressId =
            new Guid("0f988127-a99c-4b35-bd33-8bd686bedc16");

        public static readonly Guid BibleReferenceApprovingEventAddressId =
            new Guid("dac765d6-a44e-4789-960c-e8cca1a1c013");

        public static readonly Guid BibleReferenceSubmittedEventAddressId =
            new Guid("d11aa5d5-6e2d-44d2-b6de-36ed53af1f97");

        public static readonly Guid BibleReferenceApprovedEventAddressId =
            new Guid("26f1dff9-1c03-45e9-b4e3-03e754ce6a19");

        public static readonly Guid BibleReferenceRejectedEventAddressId =
            new Guid("7b0ed004-8843-4bba-a929-860eba4d18e8");

        internal static readonly IReadOnlyDictionary<BibleReferenceEventOperation, Guid>
            BibleReferenceEventAddressIds = new Dictionary<BibleReferenceEventOperation, Guid>
            {
                { BibleReferenceEventOperation.Adding, BibleReferenceAddingEventAddressId },
                { BibleReferenceEventOperation.Modifying, BibleReferenceModifyingEventAddressId },
                { BibleReferenceEventOperation.RemovingById, BibleReferenceRemovingByIdEventAddressId },

                { BibleReferenceEventOperation.HardRemovingById,
                    BibleReferenceHardRemovingByIdEventAddressId },

                { BibleReferenceEventOperation.RetrievingById, BibleReferenceRetrievingByIdEventAddressId },
                { BibleReferenceEventOperation.Submitting, BibleReferenceSubmittingEventAddressId },
                { BibleReferenceEventOperation.Approving, BibleReferenceApprovingEventAddressId },
                { BibleReferenceEventOperation.Added, BibleReferenceAddedEventAddressId },
                { BibleReferenceEventOperation.Modified, BibleReferenceModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("BibleReferenceHardRemoved" vs "BibleReferenceRemoved").
                { BibleReferenceEventOperation.Removed, BibleReferenceRemovedEventAddressId },
                { BibleReferenceEventOperation.HardRemoved, BibleReferenceRemovedEventAddressId },

                { BibleReferenceEventOperation.Submitted, BibleReferenceSubmittedEventAddressId },
                { BibleReferenceEventOperation.Approved, BibleReferenceApprovedEventAddressId },
                { BibleReferenceEventOperation.Rejected, BibleReferenceRejectedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> BibleReferenceEventAddresses =
            new Dictionary<Guid, string>
            {
                { BibleReferenceAddingEventAddressId, "BibleReference-Adding" },
                { BibleReferenceModifyingEventAddressId, "BibleReference-Modifying" },
                { BibleReferenceRemovingByIdEventAddressId, "BibleReference-RemovingById" },
                { BibleReferenceHardRemovingByIdEventAddressId, "BibleReference-HardRemovingById" },
                { BibleReferenceRetrievingByIdEventAddressId, "BibleReference-RetrievingById" },
                { BibleReferenceSubmittingEventAddressId, "BibleReference-Submitting" },
                { BibleReferenceApprovingEventAddressId, "BibleReference-Approving" },
                { BibleReferenceAddedEventAddressId, "BibleReference-Added" },
                { BibleReferenceModifiedEventAddressId, "BibleReference-Modified" },
                { BibleReferenceRemovedEventAddressId, "BibleReference-Removed" },
                { BibleReferenceSubmittedEventAddressId, "BibleReference-Submitted" },
                { BibleReferenceApprovedEventAddressId, "BibleReference-Approved" },
                { BibleReferenceRejectedEventAddressId, "BibleReference-Rejected" }
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
        public static readonly Guid BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionId =
            new Guid("019f855d-4513-7afa-8aca-c7f83447f545");

        public const string BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName =
            "BibleReferenceService.OnHardRemovingBibleReferenceById";

        public static readonly Guid BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionId =
            new Guid("019f8170-a642-772f-b896-2233aca4eb26");

        public const string BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionName =
            "BibleReferenceService.OnRetrievingBibleReferenceById";

        public static readonly Guid BibleReferenceOnSubmittingBibleReferenceSubscriptionId =
            new Guid("244d78f5-61b1-4bc3-a34b-7820cf5dcffe");

        public const string BibleReferenceOnSubmittingBibleReferenceSubscriptionName =
            "BibleReferenceService.OnSubmittingBibleReference";

        public static readonly Guid BibleReferenceOnApprovingBibleReferenceSubscriptionId =
            new Guid("cb22cb6e-1267-4f3c-b681-cfe8b35bea15");

        public const string BibleReferenceOnApprovingBibleReferenceSubscriptionName =
            "BibleReferenceService.OnApprovingBibleReference";
    }
}
