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
        public static readonly Guid AssociationAddingEventAddressId =
            new Guid("019f814e-89c1-7478-a47c-e4f34ca752c2");

        public static readonly Guid AssociationModifyingEventAddressId =
            new Guid("019f814e-89c1-73ae-b402-3079db44172d");

        public static readonly Guid AssociationRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-74c4-92b6-f9578ee44c11");

        public static readonly Guid AssociationHardRemovingByIdEventAddressId =
            new Guid("019f855d-4516-7bab-8ed1-00765c36a128");

        public static readonly Guid AssociationRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7882-b3a5-7fc91878d31b");

        public static readonly Guid AssociationAddedEventAddressId =
            new Guid("019f814e-89c1-7ac2-b650-d11879294256");

        public static readonly Guid AssociationModifiedEventAddressId =
            new Guid("019f814e-89c1-7adf-9529-4b021ea0c22e");

        public static readonly Guid AssociationRemovedEventAddressId =
            new Guid("019f814e-89c1-7e44-8fe2-bd2d85f21c3d");

        // ── State-transition addresses (design §9.7.1, §9.2) ──────────────────────────
        //
        // Every transition gets its OWN request/fact pair rather than sharing the modify
        // pair. A consumer that wants approvals must be able to subscribe to approvals
        // alone, and — more sharply — the approval workflow subscribes to Modified, so a
        // transition published there would re-enter the handler that caused it.
        //
        // Tense carries direction, per the §10.2 naming contract: the present participle is
        // the request, the past participle is the fact. The doc named only the fact half for
        // sort, confidence and scope; the request halves are chosen here to mirror the method
        // name, the way Adding/Modifying already do.

        public static readonly Guid AssociationApprovingEventAddressId =
            new Guid("019fd991-a27d-7011-bcc7-e40517d1b14e");

        public static readonly Guid AssociationApprovedEventAddressId =
            new Guid("019fd991-a27e-764a-84f9-4732feae6587");

        public static readonly Guid AssociationRejectedEventAddressId =
            new Guid("019fdd70-1b86-76cd-83ee-c46b10b2a6b0");

        // A fact address with no request address behind it, because an association has no
        // submit verb: a row reaches Submitted on add, or through the §9.2 modify carve-out.
        // What publishes here is the Administrators override re-opening a decided row (§8.6 HR-4), and
        // the approval workflow needs to hear that the round has re-opened — a demoted row that
        // announced nothing would leave every subscriber believing the old verdict still stood.
        public static readonly Guid AssociationSubmittedEventAddressId =
            new Guid("019ff3c8-4b21-7a55-9d84-2c61f0e7a913");

        // Sort has a fact address but no request address: its signature needs an anchor and a
        // side, and an envelope carries one entity. See AssociationEventOperation.
        public static readonly Guid AssociationSortedEventAddressId =
            new Guid("019fd991-a280-76fa-b6bb-92ccd5da13da");

        public static readonly Guid AssociationSettingConfidenceEventAddressId =
            new Guid("019fd991-a281-7511-a958-35585705a5bd");

        public static readonly Guid AssociationConfidenceSetEventAddressId =
            new Guid("019fd991-a282-7170-879f-e07a04235e69");

        public static readonly Guid AssociationSettingScopeEventAddressId =
            new Guid("019fd991-a283-76c0-bd32-f7b79eb5694a");

        public static readonly Guid AssociationScopedEventAddressId =
            new Guid("019fd991-a284-73b0-b736-5fe28310fb62");

        internal static readonly IReadOnlyDictionary<AssociationEventOperation, Guid>
            AssociationEventAddressIds = new Dictionary<AssociationEventOperation, Guid>
            {
                { AssociationEventOperation.Adding, AssociationAddingEventAddressId },
                { AssociationEventOperation.Modifying, AssociationModifyingEventAddressId },
                { AssociationEventOperation.RemovingById, AssociationRemovingByIdEventAddressId },
                { AssociationEventOperation.HardRemovingById, AssociationHardRemovingByIdEventAddressId },
                { AssociationEventOperation.RetrievingById, AssociationRetrievingByIdEventAddressId },
                { AssociationEventOperation.Added, AssociationAddedEventAddressId },
                { AssociationEventOperation.Modified, AssociationModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals by
                // the composed event name ("AssociationHardRemoved" vs
                // "AssociationRemoved").
                { AssociationEventOperation.Removed, AssociationRemovedEventAddressId },
                { AssociationEventOperation.HardRemoved, AssociationRemovedEventAddressId },

                { AssociationEventOperation.Approving, AssociationApprovingEventAddressId },
                { AssociationEventOperation.SettingConfidence, AssociationSettingConfidenceEventAddressId },
                { AssociationEventOperation.SettingScope, AssociationSettingScopeEventAddressId },
                { AssociationEventOperation.Submitted, AssociationSubmittedEventAddressId },
                { AssociationEventOperation.Approved, AssociationApprovedEventAddressId },
                { AssociationEventOperation.Rejected, AssociationRejectedEventAddressId },
                { AssociationEventOperation.Sorted, AssociationSortedEventAddressId },
                { AssociationEventOperation.ConfidenceSet, AssociationConfidenceSetEventAddressId },
                { AssociationEventOperation.Scoped, AssociationScopedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> AssociationEventAddresses =
            new Dictionary<Guid, string>
            {
                { AssociationAddingEventAddressId, "Association-Adding" },
                { AssociationModifyingEventAddressId, "Association-Modifying" },
                { AssociationRemovingByIdEventAddressId, "Association-RemovingById" },
                { AssociationHardRemovingByIdEventAddressId, "Association-HardRemovingById" },
                { AssociationRetrievingByIdEventAddressId, "Association-RetrievingById" },
                { AssociationAddedEventAddressId, "Association-Added" },
                { AssociationModifiedEventAddressId, "Association-Modified" },
                { AssociationRemovedEventAddressId, "Association-Removed" },
                { AssociationApprovingEventAddressId, "Association-Approving" },
                { AssociationSettingConfidenceEventAddressId, "Association-SettingConfidence" },
                { AssociationSettingScopeEventAddressId, "Association-SettingScope" },
                { AssociationSubmittedEventAddressId, "Association-Submitted" },
                { AssociationApprovedEventAddressId, "Association-Approved" },
                { AssociationRejectedEventAddressId, "Association-Rejected" },
                { AssociationSortedEventAddressId, "Association-Sorted" },
                { AssociationConfidenceSetEventAddressId, "Association-ConfidenceSet" },
                { AssociationScopedEventAddressId, "Association-Scoped" }
            };

        public static readonly Guid AssociationOnAddingAssociationSubscriptionId =
            new Guid("019f8170-a642-7cec-bc2e-da65a18d6c88");

        public const string AssociationOnAddingAssociationSubscriptionName =
            "AssociationService.OnAddingAssociation";
        public static readonly Guid AssociationOnModifyingAssociationSubscriptionId =
            new Guid("019f8170-a642-78f9-9aa0-1e0bb425e800");

        public const string AssociationOnModifyingAssociationSubscriptionName =
            "AssociationService.OnModifyingAssociation";
        public static readonly Guid AssociationOnRemovingAssociationByIdSubscriptionId =
            new Guid("019f8170-a642-76fd-8543-bdf4222337e8");

        public const string AssociationOnRemovingAssociationByIdSubscriptionName =
            "AssociationService.OnRemovingAssociationById";
        public static readonly Guid AssociationOnHardRemovingAssociationByIdSubscriptionId =
            new Guid("019f855d-4517-7ee8-82fc-fca27d45609e");

        public const string AssociationOnHardRemovingAssociationByIdSubscriptionName =
            "AssociationService.OnHardRemovingAssociationById";

        public static readonly Guid AssociationOnRetrievingAssociationByIdSubscriptionId =
            new Guid("019f8170-a642-7af2-a066-e99b09e67a3e");

        public const string AssociationOnRetrievingAssociationByIdSubscriptionName =
            "AssociationService.OnRetrievingAssociationById";

        public static readonly Guid AssociationOnApprovingAssociationSubscriptionId =
            new Guid("019fd991-a286-7e83-9eca-3b88e6c9698e");

        public const string AssociationOnApprovingAssociationSubscriptionName =
            "AssociationService.OnApprovingAssociation";

        public static readonly Guid AssociationOnSortingAssociationSubscriptionId =
            new Guid("019fd991-a287-7e02-b04e-71a4eacbb09b");

        public const string AssociationOnSortingAssociationSubscriptionName =
            "AssociationService.OnSortingAssociation";

        public static readonly Guid AssociationOnSettingAssociationConfidenceSubscriptionId =
            new Guid("019fd991-a288-72cd-a808-a22f023217c0");

        public const string AssociationOnSettingAssociationConfidenceSubscriptionName =
            "AssociationService.OnSettingAssociationConfidence";

        public static readonly Guid AssociationOnSettingAssociationScopeSubscriptionId =
            new Guid("019fd991-a289-7908-9866-880e5a093c75");

        public const string AssociationOnSettingAssociationScopeSubscriptionName =
            "AssociationService.OnSettingAssociationScope";
    }
}
