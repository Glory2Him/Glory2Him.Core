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
        public static readonly Guid ApprovalCommentAddingEventAddressId =
            new Guid("019f814e-89c1-7281-990b-b0568b502e4a");

        public static readonly Guid ApprovalCommentModifyingEventAddressId =
            new Guid("019f814e-89c1-7023-be6e-778a43956dac");

        public static readonly Guid ApprovalCommentRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-7c2c-92cc-ea7c74d98768");

        public static readonly Guid ApprovalCommentHardRemovingByIdEventAddressId =
            new Guid("019f855d-450a-7ed8-8d1b-7e3afa317faf");

        public static readonly Guid ApprovalCommentRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7e1b-a990-571b83c8e271");

        public static readonly Guid ApprovalCommentAddedEventAddressId =
            new Guid("019f814e-89c1-7638-9e01-8c4b6f593aff");

        public static readonly Guid ApprovalCommentModifiedEventAddressId =
            new Guid("019f814e-89c1-796f-872a-a225c42cabf7");

        public static readonly Guid ApprovalCommentRemovedEventAddressId =
            new Guid("019f814e-89c1-75a9-a79a-6c332280526c");

        // The subject is ApprovalComment, never Comment. CommentService owns a separate entity
        // with its own addresses, and the broker composes the stored event name as
        // subject + operation — "Comment-Resolving" would collide with that subject's namespace
        // and attribute this service's facts to the wrong entity.
        public static readonly Guid ApprovalCommentResolvingEventAddressId =
            new Guid("6571e6c1-e16c-48da-8bcf-9d8b3dcc7dbf");

        public static readonly Guid ApprovalCommentResolvedEventAddressId =
            new Guid("42698c5a-4c70-4564-b819-cd549041acc3");

        internal static readonly IReadOnlyDictionary<ApprovalCommentEventOperation, Guid>
            ApprovalCommentEventAddressIds = new Dictionary<ApprovalCommentEventOperation, Guid>
            {
                { ApprovalCommentEventOperation.Adding, ApprovalCommentAddingEventAddressId },
                { ApprovalCommentEventOperation.Modifying, ApprovalCommentModifyingEventAddressId },
                { ApprovalCommentEventOperation.RemovingById, ApprovalCommentRemovingByIdEventAddressId },
                { ApprovalCommentEventOperation.HardRemovingById, ApprovalCommentHardRemovingByIdEventAddressId },
                { ApprovalCommentEventOperation.RetrievingById, ApprovalCommentRetrievingByIdEventAddressId },
                { ApprovalCommentEventOperation.Resolving, ApprovalCommentResolvingEventAddressId },
                { ApprovalCommentEventOperation.Added, ApprovalCommentAddedEventAddressId },
                { ApprovalCommentEventOperation.Modified, ApprovalCommentModifiedEventAddressId },

                // HardRemoved is published to the SAME address as Removed on purpose —
                // consumers subscribe to one removal address and distinguish hard removals
                // by the composed event name ("ApprovalCommentHardRemoved" vs "ApprovalCommentRemoved").
                { ApprovalCommentEventOperation.Removed, ApprovalCommentRemovedEventAddressId },
                { ApprovalCommentEventOperation.HardRemoved, ApprovalCommentRemovedEventAddressId },

                { ApprovalCommentEventOperation.Resolved, ApprovalCommentResolvedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ApprovalCommentEventAddresses =
            new Dictionary<Guid, string>
            {
                { ApprovalCommentAddingEventAddressId, "ApprovalComment-Adding" },
                { ApprovalCommentModifyingEventAddressId, "ApprovalComment-Modifying" },
                { ApprovalCommentRemovingByIdEventAddressId, "ApprovalComment-RemovingById" },
                { ApprovalCommentHardRemovingByIdEventAddressId, "ApprovalComment-HardRemovingById" },
                { ApprovalCommentRetrievingByIdEventAddressId, "ApprovalComment-RetrievingById" },
                { ApprovalCommentAddedEventAddressId, "ApprovalComment-Added" },
                { ApprovalCommentModifiedEventAddressId, "ApprovalComment-Modified" },
                { ApprovalCommentRemovedEventAddressId, "ApprovalComment-Removed" },
                { ApprovalCommentResolvingEventAddressId, "ApprovalComment-Resolving" },
                { ApprovalCommentResolvedEventAddressId, "ApprovalComment-Resolved" }
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
        public static readonly Guid ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionId =
            new Guid("019f855d-450b-7587-8287-e3408b9754a9");

        public const string ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName =
            "ApprovalCommentService.OnHardRemovingApprovalCommentById";

        public static readonly Guid ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionId =
            new Guid("019f8170-a642-7afe-bbe0-4c54e63d1f1c");

        public const string ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionName =
            "ApprovalCommentService.OnRetrievingApprovalCommentById";

        public static readonly Guid ApprovalCommentOnResolvingApprovalCommentSubscriptionId =
            new Guid("49cd2631-4485-491f-a13c-2e84f9afa0b6");

        public const string ApprovalCommentOnResolvingApprovalCommentSubscriptionName =
            "ApprovalCommentService.OnResolvingApprovalComment";
    }
}
