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
        public static readonly Guid AIReviewerAssignmentAddingEventAddressId =
            new Guid("de269bc1-6063-484f-973c-38861a0e3d72");

        public static readonly Guid AIReviewerAssignmentModifyingEventAddressId =
            new Guid("3a3b2f1a-2809-4f98-85b3-faf779aee1ce");

        public static readonly Guid AIReviewerAssignmentRemovingByIdEventAddressId =
            new Guid("b9900ea1-0489-4f9f-9eb3-f7dd4a255888");

        public static readonly Guid AIReviewerAssignmentRetrievingByIdEventAddressId =
            new Guid("eaffb10c-5dc4-4c55-adc5-fe5095d41072");

        public static readonly Guid AIReviewerAssignmentAddedEventAddressId =
            new Guid("28fea4e8-d428-4691-b21c-7b903cd7a931");

        public static readonly Guid AIReviewerAssignmentModifiedEventAddressId =
            new Guid("71593d75-2e1f-4f76-8d68-346e3d2e9b3d");

        public static readonly Guid AIReviewerAssignmentRemovedEventAddressId =
            new Guid("dfa7064e-f7db-4ad5-b351-10d4a3c02105");

        internal static readonly IReadOnlyDictionary<AIReviewerAssignmentEventOperation, Guid>
            AIReviewerAssignmentEventAddressIds =
                new Dictionary<AIReviewerAssignmentEventOperation, Guid>
                {
                    {
                        AIReviewerAssignmentEventOperation.Adding,
                        AIReviewerAssignmentAddingEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.Modifying,
                        AIReviewerAssignmentModifyingEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.RemovingById,
                        AIReviewerAssignmentRemovingByIdEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.RetrievingById,
                        AIReviewerAssignmentRetrievingByIdEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.Added,
                        AIReviewerAssignmentAddedEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.Modified,
                        AIReviewerAssignmentModifiedEventAddressId
                    },
                    {
                        AIReviewerAssignmentEventOperation.Removed,
                        AIReviewerAssignmentRemovedEventAddressId
                    }
                };

        internal static readonly IReadOnlyDictionary<Guid, string> AIReviewerAssignmentEventAddresses =
            new Dictionary<Guid, string>
            {
                { AIReviewerAssignmentAddingEventAddressId, "AIReviewerAssignment-Adding" },
                { AIReviewerAssignmentModifyingEventAddressId, "AIReviewerAssignment-Modifying" },
                { AIReviewerAssignmentRemovingByIdEventAddressId, "AIReviewerAssignment-RemovingById" },
                { AIReviewerAssignmentRetrievingByIdEventAddressId, "AIReviewerAssignment-RetrievingById" },
                { AIReviewerAssignmentAddedEventAddressId, "AIReviewerAssignment-Added" },
                { AIReviewerAssignmentModifiedEventAddressId, "AIReviewerAssignment-Modified" },
                { AIReviewerAssignmentRemovedEventAddressId, "AIReviewerAssignment-Removed" }
            };

        public static readonly Guid AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionId =
            new Guid("0f18c0c9-99ef-463b-8ac5-bb8a6568d9bc");

        public const string AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionName =
            "AIReviewerAssignmentService.OnAddingAIReviewerAssignment";

        public static readonly Guid AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionId =
            new Guid("8c5f8bee-1a98-4e03-b23c-7adfb5f1f7ed");

        public const string AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName =
            "AIReviewerAssignmentService.OnModifyingAIReviewerAssignment";

        public static readonly Guid AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionId =
            new Guid("9adf94bc-0c5c-4802-8146-b83c1e7788bc");

        public const string AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionName =
            "AIReviewerAssignmentService.OnRemovingAIReviewerAssignmentById";

        public static readonly Guid AIReviewerAssignmentOnRetrievingAIReviewerAssignmentByIdSubscriptionId =
            new Guid("5ff9da67-a4c4-4d7f-adcd-4fbfad55e776");

        public const string AIReviewerAssignmentOnRetrievingAIReviewerAssignmentByIdSubscriptionName =
            "AIReviewerAssignmentService.OnRetrievingAIReviewerAssignmentById";
    }
}
