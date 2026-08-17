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
using Glory2Him.Core.Models.Events.Processings;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        public static readonly Guid LinkProcessingAddingEventAddressId =
            new Guid("01a01033-b7e9-7b68-9bb2-65240eb55e8e");

        public static readonly Guid LinkProcessingModifyingEventAddressId =
            new Guid("01a01033-b7ea-7dcc-80ec-f40a3df7a52c");

        public static readonly Guid LinkProcessingRemovingByIdEventAddressId =
            new Guid("01a01033-b7eb-71f9-8532-549ca2dd5862");

        public static readonly Guid LinkProcessingRetrievingByIdEventAddressId =
            new Guid("01a01033-b7ec-7e62-8de9-c064c0303991");

        public static readonly Guid LinkProcessingAddedEventAddressId =
            new Guid("01a01033-b7ed-7e63-acd5-7e3d9759a74c");

        public static readonly Guid LinkProcessingModifiedEventAddressId =
            new Guid("01a01033-b7ee-7383-a3a3-16b3b916a45f");

        public static readonly Guid LinkProcessingRemovedEventAddressId =
            new Guid("01a01033-b7ef-7356-8b46-1cfa6737799d");

        internal static readonly IReadOnlyDictionary<LinkProcessingEventOperation, Guid>
            LinkProcessingEventAddressIds = new Dictionary<LinkProcessingEventOperation, Guid>
            {
                {
                    LinkProcessingEventOperation.Adding,
                    LinkProcessingAddingEventAddressId
                },
                {
                    LinkProcessingEventOperation.Modifying,
                    LinkProcessingModifyingEventAddressId
                },
                {
                    LinkProcessingEventOperation.RemovingById,
                    LinkProcessingRemovingByIdEventAddressId
                },
                {
                    LinkProcessingEventOperation.RetrievingById,
                    LinkProcessingRetrievingByIdEventAddressId
                },
                {
                    LinkProcessingEventOperation.Added,
                    LinkProcessingAddedEventAddressId
                },
                {
                    LinkProcessingEventOperation.Modified,
                    LinkProcessingModifiedEventAddressId
                },
                {
                    LinkProcessingEventOperation.Removed,
                    LinkProcessingRemovedEventAddressId
                }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> LinkProcessingEventAddresses =
            new Dictionary<Guid, string>
            {
                { LinkProcessingAddingEventAddressId, "LinkProcessing-Adding" },
                { LinkProcessingModifyingEventAddressId, "LinkProcessing-Modifying" },
                { LinkProcessingRemovingByIdEventAddressId, "LinkProcessing-RemovingById" },
                { LinkProcessingRetrievingByIdEventAddressId, "LinkProcessing-RetrievingById" },
                { LinkProcessingAddedEventAddressId, "LinkProcessing-Added" },
                { LinkProcessingModifiedEventAddressId, "LinkProcessing-Modified" },
                { LinkProcessingRemovedEventAddressId, "LinkProcessing-Removed" }
            };

        public static readonly Guid LinkProcessingOnAddingLinkSubscriptionId =
            new Guid("01a01033-b7f0-7f84-9459-b9e3c16e97aa");

        public const string LinkProcessingOnAddingLinkSubscriptionName =
            "LinkProcessingService.OnAddingLink";

        public static readonly Guid LinkProcessingOnModifyingLinkSubscriptionId =
            new Guid("01a01033-b7f1-7a11-9bb7-6d9b92c6a388");

        public const string LinkProcessingOnModifyingLinkSubscriptionName =
            "LinkProcessingService.OnModifyingLink";

        public static readonly Guid LinkProcessingOnRemovingLinkByIdSubscriptionId =
            new Guid("01a01033-b7f2-787b-9604-3d6097720288");

        public const string LinkProcessingOnRemovingLinkByIdSubscriptionName =
            "LinkProcessingService.OnRemovingLinkById";

        public static readonly Guid LinkProcessingOnRetrievingLinkByIdSubscriptionId =
            new Guid("01a01033-b7f3-7421-b103-7b60147a3b93");

        public const string LinkProcessingOnRetrievingLinkByIdSubscriptionName =
            "LinkProcessingService.OnRetrievingLinkById";
    }
}
