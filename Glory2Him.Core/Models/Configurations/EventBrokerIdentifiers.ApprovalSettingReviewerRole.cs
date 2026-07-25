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
        public static readonly Guid ApprovalSettingReviewerRoleAddingEventAddressId =
            new Guid("019f9698-6fcf-72be-84c3-5c0755bb624b");

        public static readonly Guid ApprovalSettingReviewerRoleModifyingEventAddressId =
            new Guid("019f9698-6e2e-77f8-b6ba-59bc78974420");

        public static readonly Guid ApprovalSettingReviewerRoleRemovingByIdEventAddressId =
            new Guid("019f9698-6c74-7226-ac2f-fad1ec0a1e2b");

        public static readonly Guid ApprovalSettingReviewerRoleHardRemovingByIdEventAddressId =
            new Guid("019f9698-7552-7074-93a7-f0bc7aabff95");

        public static readonly Guid ApprovalSettingReviewerRoleRetrievingByIdEventAddressId =
            new Guid("019f9698-6ef7-7316-8fd8-77e4a1f48893");

        public static readonly Guid ApprovalSettingReviewerRoleAddedEventAddressId =
            new Guid("019f9698-6b69-712c-9c2a-290466847eac");

        public static readonly Guid ApprovalSettingReviewerRoleModifiedEventAddressId =
            new Guid("019f9698-6d57-7f38-a8e4-0d95d79831ef");

        public static readonly Guid ApprovalSettingReviewerRoleRemovedEventAddressId =
            new Guid("019f9698-70ea-7e34-beb6-6eba6b3fcf84");

        internal static readonly IReadOnlyDictionary<ApprovalSettingReviewerRoleEventOperation, Guid>
            ApprovalSettingReviewerRoleEventAddressIds = new Dictionary<ApprovalSettingReviewerRoleEventOperation, Guid>
            {
                { ApprovalSettingReviewerRoleEventOperation.Adding, ApprovalSettingReviewerRoleAddingEventAddressId },
                { ApprovalSettingReviewerRoleEventOperation.Modifying, ApprovalSettingReviewerRoleModifyingEventAddressId },
                { ApprovalSettingReviewerRoleEventOperation.RemovingById, ApprovalSettingReviewerRoleRemovingByIdEventAddressId },

                {
                    ApprovalSettingReviewerRoleEventOperation.HardRemovingById,
                    ApprovalSettingReviewerRoleHardRemovingByIdEventAddressId
                },

                { ApprovalSettingReviewerRoleEventOperation.RetrievingById, ApprovalSettingReviewerRoleRetrievingByIdEventAddressId },
                { ApprovalSettingReviewerRoleEventOperation.Added, ApprovalSettingReviewerRoleAddedEventAddressId },
                { ApprovalSettingReviewerRoleEventOperation.Modified, ApprovalSettingReviewerRoleModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals by
                // the composed event name ("ApprovalSettingReviewerRoleHardRemoved" vs
                // "ApprovalSettingReviewerRoleRemoved").
                { ApprovalSettingReviewerRoleEventOperation.Removed, ApprovalSettingReviewerRoleRemovedEventAddressId },
                { ApprovalSettingReviewerRoleEventOperation.HardRemoved, ApprovalSettingReviewerRoleRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalSettingReviewerRoleEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalSettingReviewerRoleAddingEventAddressId, "ApprovalSettingReviewerRole-Adding" },
                { ApprovalSettingReviewerRoleModifyingEventAddressId, "ApprovalSettingReviewerRole-Modifying" },
                { ApprovalSettingReviewerRoleRemovingByIdEventAddressId, "ApprovalSettingReviewerRole-RemovingById" },
                { ApprovalSettingReviewerRoleHardRemovingByIdEventAddressId, "ApprovalSettingReviewerRole-HardRemovingById" },
                { ApprovalSettingReviewerRoleRetrievingByIdEventAddressId, "ApprovalSettingReviewerRole-RetrievingById" },
                { ApprovalSettingReviewerRoleAddedEventAddressId, "ApprovalSettingReviewerRole-Added" },
                { ApprovalSettingReviewerRoleModifiedEventAddressId, "ApprovalSettingReviewerRole-Modified" },
                { ApprovalSettingReviewerRoleRemovedEventAddressId, "ApprovalSettingReviewerRole-Removed" }
            };

        public static readonly Guid ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionId =
            new Guid("019f9698-749a-76cf-a2c4-480b9b53f361");

        public const string ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName =
            "ApprovalSettingReviewerRoleService.OnAddingApprovalSettingReviewerRole";
        public static readonly Guid ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionId =
            new Guid("019f9698-7370-73af-8d9e-e219fcb3ae11");

        public const string ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName =
            "ApprovalSettingReviewerRoleService.OnModifyingApprovalSettingReviewerRole";
        public static readonly Guid ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionId =
            new Guid("019f9698-725d-7b85-a329-02b09fff2420");

        public const string ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName =
            "ApprovalSettingReviewerRoleService.OnRemovingApprovalSettingReviewerRoleById";
        public static readonly Guid ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionId =
            new Guid("019f9698-7610-70e7-bde3-4ac9e1742d70");

        public const string ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName =
            "ApprovalSettingReviewerRoleService.OnHardRemovingApprovalSettingReviewerRoleById";

        public static readonly Guid ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionId =
            new Guid("019f9698-7191-742d-878a-2efe7a6b1468");

        public const string ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionName =
            "ApprovalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleById";
    }
}
