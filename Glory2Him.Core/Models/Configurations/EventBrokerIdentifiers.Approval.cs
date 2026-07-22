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
        public static readonly Guid ApprovalAddingEventAddressId =
            new Guid("019f814e-89c1-735b-8b54-cbefe18f364d");

        public static readonly Guid ApprovalModifyingEventAddressId =
            new Guid("019f814e-89c1-7a79-94c5-d427c149e9aa");

        public static readonly Guid ApprovalRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7f82-89c7-1403a6080b78");

        public static readonly Guid ApprovalHardRemovingByIdEventAddressId =
            new Guid("019f855d-4508-70e3-8996-1c693bcbd146");

        public static readonly Guid ApprovalRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7c0a-93ba-d0ffa72e59e6");

        public static readonly Guid ApprovalAddedEventAddressId =
            new Guid("019f814e-89c1-7adb-9d7c-bd71522fb3da");

        public static readonly Guid ApprovalModifiedEventAddressId =
            new Guid("019f814e-89c1-79e0-8eeb-e7eab19695ab");

        public static readonly Guid ApprovalRemovedEventAddressId =
            new Guid("019f814e-89c1-70c9-a3f9-8597abe59efe");

        internal static readonly IReadOnlyDictionary<ApprovalEventOperation, Guid>
            ApprovalEventAddressIds = new Dictionary<ApprovalEventOperation, Guid>
            {
                { ApprovalEventOperation.Adding, ApprovalAddingEventAddressId },
                { ApprovalEventOperation.Modifying, ApprovalModifyingEventAddressId },
                { ApprovalEventOperation.RemovingById, ApprovalRemovingByIdEventAddressId },
                { ApprovalEventOperation.HardRemovingById, ApprovalHardRemovingByIdEventAddressId },
                { ApprovalEventOperation.RetrievingById, ApprovalRetrievingByIdEventAddressId },
                { ApprovalEventOperation.Added, ApprovalAddedEventAddressId },
                { ApprovalEventOperation.Modified, ApprovalModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("ApprovalHardRemoved" vs "ApprovalRemoved").
                { ApprovalEventOperation.Removed, ApprovalRemovedEventAddressId },
                { ApprovalEventOperation.HardRemoved, ApprovalRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalAddingEventAddressId, "Approval-Adding" },
                { ApprovalModifyingEventAddressId, "Approval-Modifying" },
                { ApprovalRemovingByIdEventAddressId, "Approval-RemovingById" },
                { ApprovalHardRemovingByIdEventAddressId, "Approval-HardRemovingById" },
                { ApprovalRetrievingByIdEventAddressId, "Approval-RetrievingById" },
                { ApprovalAddedEventAddressId, "Approval-Added" },
                { ApprovalModifiedEventAddressId, "Approval-Modified" },
                { ApprovalRemovedEventAddressId, "Approval-Removed" }
            };

        public static readonly Guid ApprovalOnAddingApprovalSubscriptionId =
            new Guid("019f8170-a642-7728-bbd7-d8cd4292cfa4");

        public const string ApprovalOnAddingApprovalSubscriptionName =
            "ApprovalService.OnAddingApproval";
        public static readonly Guid ApprovalOnModifyingApprovalSubscriptionId =
            new Guid("019f8170-a642-71f4-86a7-d0eedde5674b");

        public const string ApprovalOnModifyingApprovalSubscriptionName =
            "ApprovalService.OnModifyingApproval";
        public static readonly Guid ApprovalOnRemovingApprovalByIdSubscriptionId =
            new Guid("019f8170-a642-7093-ab40-2f32c8e1d542");

        public const string ApprovalOnRemovingApprovalByIdSubscriptionName =
            "ApprovalService.OnRemovingApprovalById";
        public static readonly Guid ApprovalOnHardRemovingApprovalByIdSubscriptionId =
            new Guid("019f855d-4509-7d5b-89c7-d70fb3d4f26d");

        public const string ApprovalOnHardRemovingApprovalByIdSubscriptionName =
            "ApprovalService.OnHardRemovingApprovalById";

        public static readonly Guid ApprovalOnRetrievingApprovalByIdSubscriptionId =
            new Guid("019f8170-a642-7266-b0d0-75428735f16f");

        public const string ApprovalOnRetrievingApprovalByIdSubscriptionName =
            "ApprovalService.OnRetrievingApprovalById";
    }
}
