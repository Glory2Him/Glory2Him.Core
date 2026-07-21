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
        public static readonly Guid ApprovalCommentAddingEventAddressId =
            new Guid("019f814e-89c1-7281-990b-b0568b502e4a");

        public static readonly Guid ApprovalCommentModifyingEventAddressId =
            new Guid("019f814e-89c1-7023-be6e-778a43956dac");

        public static readonly Guid ApprovalCommentRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7c2c-92cc-ea7c74d98768");

        public static readonly Guid ApprovalCommentRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7e1b-a990-571b83c8e271");

        public static readonly Guid ApprovalCommentAddedEventAddressId =
            new Guid("019f814e-89c1-7638-9e01-8c4b6f593aff");

        public static readonly Guid ApprovalCommentModifiedEventAddressId =
            new Guid("019f814e-89c1-796f-872a-a225c42cabf7");

        public static readonly Guid ApprovalCommentRemovedEventAddressId =
            new Guid("019f814e-89c1-75a9-a79a-6c332280526c");

        internal static readonly IReadOnlyDictionary<ApprovalCommentEventOperation, Guid>
            ApprovalCommentEventAddressIds = new Dictionary<ApprovalCommentEventOperation, Guid>
            {
                { ApprovalCommentEventOperation.Adding, ApprovalCommentAddingEventAddressId },
                { ApprovalCommentEventOperation.Modifying, ApprovalCommentModifyingEventAddressId },
                { ApprovalCommentEventOperation.RemovingById, ApprovalCommentRemovingByIdEventAddressId },
                { ApprovalCommentEventOperation.RetrievingById, ApprovalCommentRetrievingByIdEventAddressId },
                { ApprovalCommentEventOperation.Added, ApprovalCommentAddedEventAddressId },
                { ApprovalCommentEventOperation.Modified, ApprovalCommentModifiedEventAddressId },
                { ApprovalCommentEventOperation.Removed, ApprovalCommentRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalCommentEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalCommentAddingEventAddressId, "ApprovalComment-Adding" },
                { ApprovalCommentModifyingEventAddressId, "ApprovalComment-Modifying" },
                { ApprovalCommentRemovingByIdEventAddressId, "ApprovalComment-RemovingById" },
                { ApprovalCommentRetrievingByIdEventAddressId, "ApprovalComment-RetrievingById" },
                { ApprovalCommentAddedEventAddressId, "ApprovalComment-Added" },
                { ApprovalCommentModifiedEventAddressId, "ApprovalComment-Modified" },
                { ApprovalCommentRemovedEventAddressId, "ApprovalComment-Removed" }
            };

        public static readonly Guid ApprovalCommentOnAddingApprovalCommentSubscriptionId =
            new Guid("019f8170-a642-7187-bf66-b86068e319b8");

        public const string ApprovalCommentOnAddingApprovalCommentSubscriptionName =
            "ApprovalCommentService.OnAddingApprovalComment";
        public static readonly Guid ApprovalCommentOnModifyingApprovalCommentSubscriptionId =
            new Guid("019f8170-a642-7e7a-bc63-60dd2090b263");

        public const string ApprovalCommentOnModifyingApprovalCommentSubscriptionName =
            "ApprovalCommentService.OnModifyingApprovalComment";
        public static readonly Guid ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionId =
            new Guid("019f8170-a642-7e58-a11a-d1fa399120b7");

        public const string ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName =
            "ApprovalCommentService.OnRemovingApprovalCommentById";
        public static readonly Guid ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionId =
            new Guid("019f8170-a642-7afe-bbe0-4c54e63d1f1c");

        public const string ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionName =
            "ApprovalCommentService.OnRetrievingApprovalCommentById";
    }
}
