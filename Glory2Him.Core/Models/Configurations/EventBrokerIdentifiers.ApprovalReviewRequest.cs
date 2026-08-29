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
        public static readonly Guid ApprovalReviewRequestAddingEventAddressId =
            new Guid("b947b0c7-4eaa-4432-8902-9f0e341e44c8");

        public static readonly Guid ApprovalReviewRequestRemovingByIdEventAddressId =
            new Guid("236542fc-e7a4-4c5b-b98b-9cf3c5cfc440");

        public static readonly Guid ApprovalReviewRequestHardRemovingByIdEventAddressId =
            new Guid("61fcde5c-104d-488c-b1b7-25e40b5cf8c9");

        public static readonly Guid ApprovalReviewRequestRetrievingByIdEventAddressId =
            new Guid("1c77f1e3-c91e-4b5e-8acf-9535ce0b81e7");

        // The address §7.9 rule 8 names as the hook a future notification feature subscribes to,
        // so the invited user learns they have been asked. Nothing consumes it today.
        public static readonly Guid ApprovalReviewRequestAddedEventAddressId =
            new Guid("f0c47734-5524-472b-a373-b03ac746f088");

        public static readonly Guid ApprovalReviewRequestRemovedEventAddressId =
            new Guid("019c1e10-058a-4a73-ad61-c234a9db52e7");

        internal static readonly IReadOnlyDictionary<ApprovalReviewRequestEventOperation, Guid>
            ApprovalReviewRequestEventAddressIds =
                new Dictionary<ApprovalReviewRequestEventOperation, Guid>
                {
                    {
                        ApprovalReviewRequestEventOperation.Adding,
                        ApprovalReviewRequestAddingEventAddressId
                    },
                    {
                        ApprovalReviewRequestEventOperation.RemovingById,
                        ApprovalReviewRequestRemovingByIdEventAddressId
                    },
                    {
                        ApprovalReviewRequestEventOperation.HardRemovingById,
                        ApprovalReviewRequestHardRemovingByIdEventAddressId
                    },
                    {
                        ApprovalReviewRequestEventOperation.RetrievingById,
                        ApprovalReviewRequestRetrievingByIdEventAddressId
                    },
                    {
                        ApprovalReviewRequestEventOperation.Added,
                        ApprovalReviewRequestAddedEventAddressId
                    },

                    // HardRemoved is published to the SAME address as Removed on purpose —
                    // consumers subscribe to one removal address and distinguish hard removals by
                    // the composed event name ("ApprovalReviewRequestHardRemoved" vs
                    // "ApprovalReviewRequestRemoved").
                    {
                        ApprovalReviewRequestEventOperation.Removed,
                        ApprovalReviewRequestRemovedEventAddressId
                    },
                    {
                        ApprovalReviewRequestEventOperation.HardRemoved,
                        ApprovalReviewRequestRemovedEventAddressId
                    }
                };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalReviewRequestEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalReviewRequestAddingEventAddressId, "ApprovalReviewRequest-Adding" },
                { ApprovalReviewRequestRemovingByIdEventAddressId, "ApprovalReviewRequest-RemovingById" },

                {
                    ApprovalReviewRequestHardRemovingByIdEventAddressId,
                    "ApprovalReviewRequest-HardRemovingById"
                },

                { ApprovalReviewRequestRetrievingByIdEventAddressId, "ApprovalReviewRequest-RetrievingById" },
                { ApprovalReviewRequestAddedEventAddressId, "ApprovalReviewRequest-Added" },
                { ApprovalReviewRequestRemovedEventAddressId, "ApprovalReviewRequest-Removed" }
            };

        public static readonly Guid ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionId =
            new Guid("3a27d08a-b518-43dd-80d3-c82fdb63afa5");

        public const string ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName =
            "ApprovalReviewRequestService.OnAddingApprovalReviewRequest";

        public static readonly Guid ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionId =
            new Guid("c7c4877b-9890-4fc2-bb67-000e4accf3c6");

        public const string ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName =
            "ApprovalReviewRequestService.OnRemovingApprovalReviewRequestById";

        public static readonly Guid ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionId =
            new Guid("d281fe34-570d-44b1-b4c0-0822ed5f8eb8");

        public const string ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName =
            "ApprovalReviewRequestService.OnHardRemovingApprovalReviewRequestById";

        public static readonly Guid ApprovalReviewRequestOnRetrievingApprovalReviewRequestByIdSubscriptionId =
            new Guid("6bc81e00-1d3d-4395-b71b-abe1836252fa");

        public const string ApprovalReviewRequestOnRetrievingApprovalReviewRequestByIdSubscriptionName =
            "ApprovalReviewRequestService.OnRetrievingApprovalReviewRequestById";
    }
}
