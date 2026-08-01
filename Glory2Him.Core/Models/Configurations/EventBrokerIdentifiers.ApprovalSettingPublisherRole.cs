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
        public static readonly Guid ApprovalSettingPublisherRoleAddingEventAddressId =
            new Guid("019f9698-7b51-707f-be76-1961af477fa9");

        public static readonly Guid ApprovalSettingPublisherRoleModifyingEventAddressId =
            new Guid("019f9698-79ec-7ae3-9afe-f2e2c84eec80");

        public static readonly Guid ApprovalSettingPublisherRoleRemovingByIdEventAddressId =
            new Guid("019f9698-783a-755f-9068-577b923db600");

        public static readonly Guid ApprovalSettingPublisherRoleHardRemovingByIdEventAddressId =
            new Guid("019f9698-8055-70d0-a6cd-05a2cca569d0");

        public static readonly Guid ApprovalSettingPublisherRoleRetrievingByIdEventAddressId =
            new Guid("019f9698-7aa7-77fb-b5af-7c3a6e217a11");

        public static readonly Guid ApprovalSettingPublisherRoleAddedEventAddressId =
            new Guid("019f9698-771a-73bc-b9ef-b6e3b1986ee9");

        public static readonly Guid ApprovalSettingPublisherRoleModifiedEventAddressId =
            new Guid("019f9698-7923-763a-afcb-815357cbdd26");

        public static readonly Guid ApprovalSettingPublisherRoleRemovedEventAddressId =
            new Guid("019f9698-7c19-7bee-a619-c85fe1ed40e0");

        internal static readonly IReadOnlyDictionary<ApprovalSettingPublisherRoleEventOperation, Guid>
            ApprovalSettingPublisherRoleEventAddressIds = new Dictionary<ApprovalSettingPublisherRoleEventOperation, Guid>
            {
                { ApprovalSettingPublisherRoleEventOperation.Adding, ApprovalSettingPublisherRoleAddingEventAddressId },
                { ApprovalSettingPublisherRoleEventOperation.Modifying, ApprovalSettingPublisherRoleModifyingEventAddressId },
                { ApprovalSettingPublisherRoleEventOperation.RemovingById, ApprovalSettingPublisherRoleRemovingByIdEventAddressId },

                {
                    ApprovalSettingPublisherRoleEventOperation.HardRemovingById,
                    ApprovalSettingPublisherRoleHardRemovingByIdEventAddressId
                },

                { ApprovalSettingPublisherRoleEventOperation.RetrievingById, ApprovalSettingPublisherRoleRetrievingByIdEventAddressId },
                { ApprovalSettingPublisherRoleEventOperation.Added, ApprovalSettingPublisherRoleAddedEventAddressId },
                { ApprovalSettingPublisherRoleEventOperation.Modified, ApprovalSettingPublisherRoleModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals by
                // the composed event name ("ApprovalSettingPublisherRoleHardRemoved" vs
                // "ApprovalSettingPublisherRoleRemoved").
                { ApprovalSettingPublisherRoleEventOperation.Removed, ApprovalSettingPublisherRoleRemovedEventAddressId },
                { ApprovalSettingPublisherRoleEventOperation.HardRemoved, ApprovalSettingPublisherRoleRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalSettingPublisherRoleEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalSettingPublisherRoleAddingEventAddressId, "ApprovalSettingPublisherRole-Adding" },
                { ApprovalSettingPublisherRoleModifyingEventAddressId, "ApprovalSettingPublisherRole-Modifying" },
                { ApprovalSettingPublisherRoleRemovingByIdEventAddressId, "ApprovalSettingPublisherRole-RemovingById" },
                { ApprovalSettingPublisherRoleHardRemovingByIdEventAddressId, "ApprovalSettingPublisherRole-HardRemovingById" },
                { ApprovalSettingPublisherRoleRetrievingByIdEventAddressId, "ApprovalSettingPublisherRole-RetrievingById" },
                { ApprovalSettingPublisherRoleAddedEventAddressId, "ApprovalSettingPublisherRole-Added" },
                { ApprovalSettingPublisherRoleModifiedEventAddressId, "ApprovalSettingPublisherRole-Modified" },
                { ApprovalSettingPublisherRoleRemovedEventAddressId, "ApprovalSettingPublisherRole-Removed" }
            };

        public static readonly Guid ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionId =
            new Guid("019f9698-7f8c-71fd-baf0-a2f4f3a2d282");

        public const string ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName =
            "ApprovalSettingPublisherRoleService.OnAddingApprovalSettingPublisherRole";
        public static readonly Guid ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionId =
            new Guid("019f9698-7ec5-749d-8fb1-7e5fd6828b5a");

        public const string ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName =
            "ApprovalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRole";
        public static readonly Guid ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionId =
            new Guid("019f9698-7dde-786f-95a6-13aa16b34116");

        public const string ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName =
            "ApprovalSettingPublisherRoleService.OnRemovingApprovalSettingPublisherRoleById";
        public static readonly Guid ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionId =
            new Guid("019f9698-80fe-7c60-981f-7d84f5d27e5f");

        public const string ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName =
            "ApprovalSettingPublisherRoleService.OnHardRemovingApprovalSettingPublisherRoleById";

        public static readonly Guid ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionId =
            new Guid("019f9698-7ce5-76c5-9a9b-2744aad6dcfb");

        public const string ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionName =
            "ApprovalSettingPublisherRoleService.OnRetrievingApprovalSettingPublisherRoleById";
    }
}
