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
        public static readonly Guid AttachmentAddingEventAddressId =
            new Guid("019f814e-89c1-7431-9ab6-52d6a22c6ed4");

        public static readonly Guid AttachmentModifyingEventAddressId =
            new Guid("019f814e-89c1-7d06-86a2-a871b72d91e3");

        public static readonly Guid AttachmentRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-727b-93bf-2300c9fc87a9");

        public static readonly Guid AttachmentRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7c19-bdc2-18a42de25290");

        public static readonly Guid AttachmentAddedEventAddressId =
            new Guid("019f814e-89c1-7221-8c4d-4c697c6a6f2e");

        public static readonly Guid AttachmentModifiedEventAddressId =
            new Guid("019f814e-89c1-732a-94bc-ad5b0cadda3a");

        public static readonly Guid AttachmentRemovedEventAddressId =
            new Guid("019f814e-89c1-7e90-ac32-e37eb810d9ce");

        internal static readonly IReadOnlyDictionary<AttachmentEventOperation, Guid>
            AttachmentEventAddressIds = new Dictionary<AttachmentEventOperation, Guid>
            {
                { AttachmentEventOperation.Adding, AttachmentAddingEventAddressId },
                { AttachmentEventOperation.Modifying, AttachmentModifyingEventAddressId },
                { AttachmentEventOperation.RemovingById, AttachmentRemovingByIdEventAddressId },
                { AttachmentEventOperation.RetrievingById, AttachmentRetrievingByIdEventAddressId },
                { AttachmentEventOperation.Added, AttachmentAddedEventAddressId },
                { AttachmentEventOperation.Modified, AttachmentModifiedEventAddressId },
                { AttachmentEventOperation.Removed, AttachmentRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> AttachmentEventAddresses =
            new Dictionary<Guid, string>
            {
                { AttachmentAddingEventAddressId, "Attachment-Adding" },
                { AttachmentModifyingEventAddressId, "Attachment-Modifying" },
                { AttachmentRemovingByIdEventAddressId, "Attachment-RemovingById" },
                { AttachmentRetrievingByIdEventAddressId, "Attachment-RetrievingById" },
                { AttachmentAddedEventAddressId, "Attachment-Added" },
                { AttachmentModifiedEventAddressId, "Attachment-Modified" },
                { AttachmentRemovedEventAddressId, "Attachment-Removed" }
            };

        public static readonly Guid AttachmentOnAddingAttachmentSubscriptionId =
            new Guid("019f8170-a642-73ef-a3f9-c4ea0dfe06e9");

        public const string AttachmentOnAddingAttachmentSubscriptionName =
            "AttachmentService.OnAddingAttachment";
        public static readonly Guid AttachmentOnModifyingAttachmentSubscriptionId =
            new Guid("019f8170-a642-77b2-b2c1-29cc5ddb9681");

        public const string AttachmentOnModifyingAttachmentSubscriptionName =
            "AttachmentService.OnModifyingAttachment";
        public static readonly Guid AttachmentOnRemovingAttachmentByIdSubscriptionId =
            new Guid("019f8170-a642-71e8-a3ea-908d20f9bac2");

        public const string AttachmentOnRemovingAttachmentByIdSubscriptionName =
            "AttachmentService.OnRemovingAttachmentById";
        public static readonly Guid AttachmentOnRetrievingAttachmentByIdSubscriptionId =
            new Guid("019f8170-a642-7c23-8648-4279a3ca3e8a");

        public const string AttachmentOnRetrievingAttachmentByIdSubscriptionName =
            "AttachmentService.OnRetrievingAttachmentById";
    }
}
