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
        public static readonly Guid ApprovalReviewAddingEventAddressId =
            new Guid("019f814e-89c1-7bf9-bdb8-c45b87d10399");

        public static readonly Guid ApprovalReviewModifyingEventAddressId =
            new Guid("019f814e-89c1-7e2c-9bbb-c62623fe955b");

        public static readonly Guid ApprovalReviewRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7f1a-a99c-8dc6f831cc64");

        public static readonly Guid ApprovalReviewHardRemovingByIdEventAddressId =
            new Guid("019f855d-450c-7fcb-8e70-b829ae92014c");

        public static readonly Guid ApprovalReviewRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-79cd-a33f-664f2a7bb0e1");

        public static readonly Guid ApprovalReviewAddedEventAddressId =
            new Guid("019f814e-89c1-7625-b9d5-736457bfe959");

        public static readonly Guid ApprovalReviewModifiedEventAddressId =
            new Guid("019f814e-89c1-772f-bc6c-842ccdb4cbe1");

        public static readonly Guid ApprovalReviewRemovedEventAddressId =
            new Guid("019f814e-89c1-7afc-b8ea-799112e620d3");

        internal static readonly IReadOnlyDictionary<ApprovalReviewEventOperation, Guid>
            ApprovalReviewEventAddressIds = new Dictionary<ApprovalReviewEventOperation, Guid>
            {
                { ApprovalReviewEventOperation.Adding, ApprovalReviewAddingEventAddressId },
                { ApprovalReviewEventOperation.Modifying, ApprovalReviewModifyingEventAddressId },
                { ApprovalReviewEventOperation.RemovingById, ApprovalReviewRemovingByIdEventAddressId },
                { ApprovalReviewEventOperation.HardRemovingById, ApprovalReviewHardRemovingByIdEventAddressId },
                { ApprovalReviewEventOperation.RetrievingById, ApprovalReviewRetrievingByIdEventAddressId },
                { ApprovalReviewEventOperation.Added, ApprovalReviewAddedEventAddressId },
                { ApprovalReviewEventOperation.Modified, ApprovalReviewModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("ApprovalReviewHardRemoved" vs "ApprovalReviewRemoved").
                { ApprovalReviewEventOperation.Removed, ApprovalReviewRemovedEventAddressId },
                { ApprovalReviewEventOperation.HardRemoved, ApprovalReviewRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalReviewEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalReviewAddingEventAddressId, "ApprovalReview-Adding" },
                { ApprovalReviewModifyingEventAddressId, "ApprovalReview-Modifying" },
                { ApprovalReviewRemovingByIdEventAddressId, "ApprovalReview-RemovingById" },
                { ApprovalReviewHardRemovingByIdEventAddressId, "ApprovalReview-HardRemovingById" },
                { ApprovalReviewRetrievingByIdEventAddressId, "ApprovalReview-RetrievingById" },
                { ApprovalReviewAddedEventAddressId, "ApprovalReview-Added" },
                { ApprovalReviewModifiedEventAddressId, "ApprovalReview-Modified" },
                { ApprovalReviewRemovedEventAddressId, "ApprovalReview-Removed" }
            };

        public static readonly Guid ApprovalReviewOnAddingApprovalReviewSubscriptionId =
            new Guid("019f8170-a642-7828-9a83-7c243664bd19");

        public const string ApprovalReviewOnAddingApprovalReviewSubscriptionName =
            "ApprovalReviewService.OnAddingApprovalReview";
        public static readonly Guid ApprovalReviewOnModifyingApprovalReviewSubscriptionId =
            new Guid("019f8170-a642-7880-95d0-b58ac5229091");

        public const string ApprovalReviewOnModifyingApprovalReviewSubscriptionName =
            "ApprovalReviewService.OnModifyingApprovalReview";
        public static readonly Guid ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionId =
            new Guid("019f8170-a642-7092-9ad0-474541ae0924");

        public const string ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName =
            "ApprovalReviewService.OnRemovingApprovalReviewById";
        public static readonly Guid ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionId =
            new Guid("019f855d-450d-73ad-8166-5309650cffb6");

        public const string ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName =
            "ApprovalReviewService.OnHardRemovingApprovalReviewById";

        public static readonly Guid ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionId =
            new Guid("019f8170-a642-7a76-b163-e010f847e78d");

        public const string ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionName =
            "ApprovalReviewService.OnRetrievingApprovalReviewById";
    }
}
