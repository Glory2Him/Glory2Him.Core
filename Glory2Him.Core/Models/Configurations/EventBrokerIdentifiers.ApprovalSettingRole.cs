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
        public static readonly Guid ApprovalSettingRoleAddingEventAddressId =
            new Guid("019f814e-89c1-7cb5-9103-42411ae2e0cd");

        public static readonly Guid ApprovalSettingRoleModifyingEventAddressId =
            new Guid("019f814e-89c1-7728-903b-811e759d95a0");

        public static readonly Guid ApprovalSettingRoleRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-70f3-9150-9a17dd06b5b0");

        public static readonly Guid ApprovalSettingRoleRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7a17-95b6-31a03ac2be28");

        public static readonly Guid ApprovalSettingRoleAddedEventAddressId =
            new Guid("019f814e-89c1-70be-b7cb-ea0f6b767088");

        public static readonly Guid ApprovalSettingRoleModifiedEventAddressId =
            new Guid("019f814e-89c1-74b2-9b35-7b2b86dca781");

        public static readonly Guid ApprovalSettingRoleRemovedEventAddressId =
            new Guid("019f814e-89c1-7ea6-a835-daedd5d9cac2");

        internal static readonly IReadOnlyDictionary<ApprovalSettingRoleEventOperation, Guid>
            ApprovalSettingRoleEventAddressIds = new Dictionary<ApprovalSettingRoleEventOperation, Guid>
            {
                { ApprovalSettingRoleEventOperation.Adding, ApprovalSettingRoleAddingEventAddressId },
                { ApprovalSettingRoleEventOperation.Modifying, ApprovalSettingRoleModifyingEventAddressId },
                { ApprovalSettingRoleEventOperation.RemovingById, ApprovalSettingRoleRemovingByIdEventAddressId },
                { ApprovalSettingRoleEventOperation.RetrievingById, ApprovalSettingRoleRetrievingByIdEventAddressId },
                { ApprovalSettingRoleEventOperation.Added, ApprovalSettingRoleAddedEventAddressId },
                { ApprovalSettingRoleEventOperation.Modified, ApprovalSettingRoleModifiedEventAddressId },
                { ApprovalSettingRoleEventOperation.Removed, ApprovalSettingRoleRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalSettingRoleEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalSettingRoleAddingEventAddressId, "ApprovalSettingRole-Adding" },
                { ApprovalSettingRoleModifyingEventAddressId, "ApprovalSettingRole-Modifying" },
                { ApprovalSettingRoleRemovingByIdEventAddressId, "ApprovalSettingRole-RemovingById" },
                { ApprovalSettingRoleRetrievingByIdEventAddressId, "ApprovalSettingRole-RetrievingById" },
                { ApprovalSettingRoleAddedEventAddressId, "ApprovalSettingRole-Added" },
                { ApprovalSettingRoleModifiedEventAddressId, "ApprovalSettingRole-Modified" },
                { ApprovalSettingRoleRemovedEventAddressId, "ApprovalSettingRole-Removed" }
            };

        public static readonly Guid ApprovalSettingRoleOnAddingApprovalSettingRoleSubscriptionId =
            new Guid("019f8170-a642-7e0a-ad0a-d283cd4891de");

        public const string ApprovalSettingRoleOnAddingApprovalSettingRoleSubscriptionName =
            "ApprovalSettingRoleService.OnAddingApprovalSettingRole";
        public static readonly Guid ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionId =
            new Guid("019f8170-a642-79c9-9a3b-d8e66c52b150");

        public const string ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName =
            "ApprovalSettingRoleService.OnModifyingApprovalSettingRole";
        public static readonly Guid ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionId =
            new Guid("019f8170-a642-7469-b23d-6ae04eb831dd");

        public const string ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName =
            "ApprovalSettingRoleService.OnRemovingApprovalSettingRoleById";
        public static readonly Guid ApprovalSettingRoleOnRetrievingApprovalSettingRoleByIdSubscriptionId =
            new Guid("019f8170-a642-7227-ace2-3a85e8409142");

        public const string ApprovalSettingRoleOnRetrievingApprovalSettingRoleByIdSubscriptionName =
            "ApprovalSettingRoleService.OnRetrievingApprovalSettingRoleById";
    }
}
