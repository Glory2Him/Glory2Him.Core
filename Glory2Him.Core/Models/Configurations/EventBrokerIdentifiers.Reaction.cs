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
        public static readonly Guid ReactionAddingEventAddressId =
            new Guid("019f814e-89c1-7c63-bc4e-eea12ab72bab");

        public static readonly Guid ReactionModifyingEventAddressId =
            new Guid("019f814e-89c1-7ccc-84e5-551b60fd5002");

        public static readonly Guid ReactionRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-76ca-b0db-51e854ad03e7");

        public static readonly Guid ReactionHardRemovingByIdEventAddressId =
            new Guid("019f855d-451c-7ed5-8650-d003319aba23");

        public static readonly Guid ReactionRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-79a2-94de-bd92b5249148");

        public static readonly Guid ReactionAddedEventAddressId =
            new Guid("019f814e-89c1-7991-82a1-978857130455");

        public static readonly Guid ReactionModifiedEventAddressId =
            new Guid("019f814e-89c1-7d65-b134-8979248e0766");

        public static readonly Guid ReactionRemovedEventAddressId =
            new Guid("019f814e-89c1-7f30-9317-3385e9c5f6ae");

        public static readonly Guid ReactionSubmittingEventAddressId =
            new Guid("52b66362-df28-4dfe-bddd-a0b049bb6ead");

        public static readonly Guid ReactionApprovingEventAddressId =
            new Guid("f7e89adf-c621-4924-bcbe-73df2781abf4");

        public static readonly Guid ReactionSubmittedEventAddressId =
            new Guid("efea93a6-f810-4a81-bd7d-c952782c3868");

        public static readonly Guid ReactionApprovedEventAddressId =
            new Guid("ecd33d43-a567-4b5f-8e0e-7fc34fb31963");

        public static readonly Guid ReactionRejectedEventAddressId =
            new Guid("99bfcfa4-4bc4-4930-a202-aefcd32049e9");

        internal static readonly IReadOnlyDictionary<ReactionEventOperation, Guid>
            ReactionEventAddressIds = new Dictionary<ReactionEventOperation, Guid>
            {
                { ReactionEventOperation.Adding, ReactionAddingEventAddressId },
                { ReactionEventOperation.Modifying, ReactionModifyingEventAddressId },
                { ReactionEventOperation.RemovingById, ReactionRemovingByIdEventAddressId },
                { ReactionEventOperation.HardRemovingById, ReactionHardRemovingByIdEventAddressId },
                { ReactionEventOperation.RetrievingById, ReactionRetrievingByIdEventAddressId },
                { ReactionEventOperation.Submitting, ReactionSubmittingEventAddressId },
                { ReactionEventOperation.Approving, ReactionApprovingEventAddressId },
                { ReactionEventOperation.Added, ReactionAddedEventAddressId },
                { ReactionEventOperation.Modified, ReactionModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("ReactionHardRemoved" vs "ReactionRemoved").
                { ReactionEventOperation.Removed, ReactionRemovedEventAddressId },
                { ReactionEventOperation.HardRemoved, ReactionRemovedEventAddressId },

                { ReactionEventOperation.Submitted, ReactionSubmittedEventAddressId },
                { ReactionEventOperation.Approved, ReactionApprovedEventAddressId },
                { ReactionEventOperation.Rejected, ReactionRejectedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ReactionEventAddresses =
            new Dictionary<Guid, string>
            {
                { ReactionAddingEventAddressId, "Reaction-Adding" },
                { ReactionModifyingEventAddressId, "Reaction-Modifying" },
                { ReactionRemovingByIdEventAddressId, "Reaction-RemovingById" },
                { ReactionHardRemovingByIdEventAddressId, "Reaction-HardRemovingById" },
                { ReactionRetrievingByIdEventAddressId, "Reaction-RetrievingById" },
                { ReactionSubmittingEventAddressId, "Reaction-Submitting" },
                { ReactionApprovingEventAddressId, "Reaction-Approving" },
                { ReactionAddedEventAddressId, "Reaction-Added" },
                { ReactionModifiedEventAddressId, "Reaction-Modified" },
                { ReactionRemovedEventAddressId, "Reaction-Removed" },
                { ReactionSubmittedEventAddressId, "Reaction-Submitted" },
                { ReactionApprovedEventAddressId, "Reaction-Approved" },
                { ReactionRejectedEventAddressId, "Reaction-Rejected" }
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
        public static readonly Guid ReactionOnHardRemovingReactionByIdSubscriptionId =
            new Guid("019f855d-451d-780e-88c8-0e561b79a782");

        public const string ReactionOnHardRemovingReactionByIdSubscriptionName =
            "ReactionService.OnHardRemovingReactionById";

        public static readonly Guid ReactionOnRetrievingReactionByIdSubscriptionId =
            new Guid("019f8170-a642-774a-adab-4abec905d9ea");

        public const string ReactionOnRetrievingReactionByIdSubscriptionName =
            "ReactionService.OnRetrievingReactionById";

        public static readonly Guid ReactionOnSubmittingReactionSubscriptionId =
            new Guid("94378f7e-568f-4960-87a4-4f2bdfeeba18");

        public const string ReactionOnSubmittingReactionSubscriptionName =
            "ReactionService.OnSubmittingReaction";

        public static readonly Guid ReactionOnApprovingReactionSubscriptionId =
            new Guid("d27e3ea0-49dc-4a5f-9318-771f9e78a306");

        public const string ReactionOnApprovingReactionSubscriptionName =
            "ReactionService.OnApprovingReaction";
    }
}
