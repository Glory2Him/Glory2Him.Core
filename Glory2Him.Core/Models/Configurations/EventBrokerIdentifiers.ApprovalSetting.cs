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
        public static readonly Guid ApprovalSettingAddingEventAddressId =
            new Guid("019f814e-89c1-71e3-a522-91b61ac98f99");

        public static readonly Guid ApprovalSettingModifyingEventAddressId =
            new Guid("019f814e-89c1-7c27-a72b-3ba09eebf134");

        public static readonly Guid ApprovalSettingRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7f8b-bfbf-6fabcd3a3f72");

        public static readonly Guid ApprovalSettingRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7e24-8861-f43c40425302");

        public static readonly Guid ApprovalSettingAddedEventAddressId =
            new Guid("019f814e-89c1-7416-a496-ad8ed8792b33");

        public static readonly Guid ApprovalSettingModifiedEventAddressId =
            new Guid("019f814e-89c1-701b-acaf-cd3d4a6869ba");

        public static readonly Guid ApprovalSettingRemovedEventAddressId =
            new Guid("019f814e-89c1-74d3-8c5b-44467d3a0a4d");

        internal static readonly IReadOnlyDictionary<ApprovalSettingEventOperation, Guid> ApprovalSettingEventAddressIds =
            new Dictionary<ApprovalSettingEventOperation, Guid>
            {
                { ApprovalSettingEventOperation.Adding, ApprovalSettingAddingEventAddressId },
                { ApprovalSettingEventOperation.Modifying, ApprovalSettingModifyingEventAddressId },
                { ApprovalSettingEventOperation.RemovingById, ApprovalSettingRemovingByIdEventAddressId },
                { ApprovalSettingEventOperation.RetrievingById, ApprovalSettingRetrievingByIdEventAddressId },
                { ApprovalSettingEventOperation.Added, ApprovalSettingAddedEventAddressId },
                { ApprovalSettingEventOperation.Modified, ApprovalSettingModifiedEventAddressId },
                { ApprovalSettingEventOperation.Removed, ApprovalSettingRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalSettingEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalSettingAddingEventAddressId, "ApprovalSetting-Adding" },
                { ApprovalSettingModifyingEventAddressId, "ApprovalSetting-Modifying" },
                { ApprovalSettingRemovingByIdEventAddressId, "ApprovalSetting-RemovingById" },
                { ApprovalSettingRetrievingByIdEventAddressId, "ApprovalSetting-RetrievingById" },
                { ApprovalSettingAddedEventAddressId, "ApprovalSetting-Added" },
                { ApprovalSettingModifiedEventAddressId, "ApprovalSetting-Modified" },
                { ApprovalSettingRemovedEventAddressId, "ApprovalSetting-Removed" }
            };

        public static readonly Guid ApprovalSettingOnAddingApprovalSettingSubscriptionId =
            new Guid("019f8170-a642-79d5-ae49-12a2e9d613bc");

        public const string ApprovalSettingOnAddingApprovalSettingSubscriptionName =
            "ApprovalSettingService.OnAddingApprovalSetting";
        public static readonly Guid ApprovalSettingOnModifyingApprovalSettingSubscriptionId =
            new Guid("019f8170-a642-7c9a-a624-fb1acbbe1391");

        public const string ApprovalSettingOnModifyingApprovalSettingSubscriptionName =
            "ApprovalSettingService.OnModifyingApprovalSetting";
        public static readonly Guid ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionId =
            new Guid("019f8170-a642-7857-9643-5497c256caf9");

        public const string ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName =
            "ApprovalSettingService.OnRemovingApprovalSettingById";
        public static readonly Guid ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionId =
            new Guid("019f8170-a642-7f8c-b207-3ceb672ecb3d");

        public const string ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionName =
            "ApprovalSettingService.OnRetrievingApprovalSettingById";
    }
}
