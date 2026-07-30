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
using Glory2Him.Core.Models.Events.Orchestrations;

namespace Glory2Him.Core.Models.Configurations
{
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid ContentItemSubmittingEventAddressId =
            new Guid("019fab26-8096-7793-9db4-b88aa480fa6e");

        internal static readonly IReadOnlyDictionary<ContentItemSubmissionEventOperation, Guid>
            ContentItemSubmissionEventAddressIds = new Dictionary<ContentItemSubmissionEventOperation, Guid>
            {
                { ContentItemSubmissionEventOperation.Submitting, ContentItemSubmittingEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> ContentItemSubmissionEventAddresses =
            new Dictionary<Guid, string>
            {
                { ContentItemSubmittingEventAddressId, "ContentItem-Submitting" }
            };

        public static readonly Guid ContentItemOrchestrationOnSubmittingContentItemSubscriptionId =
            new Guid("019fab26-8096-7007-866e-805ae4cfab7f");

        public const string ContentItemOrchestrationOnSubmittingContentItemSubscriptionName =
            "ContentItemOrchestrationService.OnSubmittingContentItem";
    }
}
