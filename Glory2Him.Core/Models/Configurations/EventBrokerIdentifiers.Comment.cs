// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Models.Configurations
{
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid CommentAddingEventAddressId =
            new Guid("019f814e-89c1-7b66-8be9-dc9edf88acfd");

        public static readonly Guid CommentModifyingEventAddressId =
            new Guid("019f814e-89c1-7165-b397-cdb4aef87b37");

        public static readonly Guid CommentRemovingByIdEventAddressId =
            new Guid("019f814e-89c1-79a9-abe0-8b1239e547e6");

        public static readonly Guid CommentRetrievingByIdEventAddressId =
            new Guid("019f814e-89c1-7d8d-8d47-595ae212afa9");

        public static readonly Guid CommentAddedEventAddressId =
            new Guid("019f814e-89c1-7d1d-8fec-740bea876ca9");

        public static readonly Guid CommentModifiedEventAddressId =
            new Guid("019f814e-89c1-7340-96e1-fb24804431db");

        public static readonly Guid CommentRemovedEventAddressId =
            new Guid("019f814e-89c1-7c8c-8e89-494580f6a5b2");

        internal static readonly IReadOnlyDictionary<CommentEventOperation, Guid> CommentEventAddressIds =
            new Dictionary<CommentEventOperation, Guid>
            {
                { CommentEventOperation.Adding, CommentAddingEventAddressId },
                { CommentEventOperation.Modifying, CommentModifyingEventAddressId },
                { CommentEventOperation.RemovingById, CommentRemovingByIdEventAddressId },
                { CommentEventOperation.RetrievingById, CommentRetrievingByIdEventAddressId },
                { CommentEventOperation.Added, CommentAddedEventAddressId },
                { CommentEventOperation.Modified, CommentModifiedEventAddressId },
                { CommentEventOperation.Removed, CommentRemovedEventAddressId }
            };

        internal static readonly IReadOnlyDictionary<Guid, string> CommentEventAddresses =
            new Dictionary<Guid, string>
            {
                { CommentAddingEventAddressId, "Comment-Adding" },
                { CommentModifyingEventAddressId, "Comment-Modifying" },
                { CommentRemovingByIdEventAddressId, "Comment-RemovingById" },
                { CommentRetrievingByIdEventAddressId, "Comment-RetrievingById" },
                { CommentAddedEventAddressId, "Comment-Added" },
                { CommentModifiedEventAddressId, "Comment-Modified" },
                { CommentRemovedEventAddressId, "Comment-Removed" }
            };

        public static readonly Guid CommentOnAddingCommentSubscriptionId =
            new Guid("019f8170-a642-7275-9bd5-7b7c95ecd7b9");

        public const string CommentOnAddingCommentSubscriptionName =
            "CommentService.OnAddingComment";
        public static readonly Guid CommentOnModifyingCommentSubscriptionId =
            new Guid("019f8170-a642-7088-8a5e-41267d1a5d9d");

        public const string CommentOnModifyingCommentSubscriptionName =
            "CommentService.OnModifyingComment";
        public static readonly Guid CommentOnRemovingCommentByIdSubscriptionId =
            new Guid("019f8170-a642-7c47-9dd7-d9f529638e23");

        public const string CommentOnRemovingCommentByIdSubscriptionName =
            "CommentService.OnRemovingCommentById";
        public static readonly Guid CommentOnRetrievingCommentByIdSubscriptionId =
            new Guid("019f8170-a642-74d9-8513-40f8c92d873c");

        public const string CommentOnRetrievingCommentByIdSubscriptionName =
            "CommentService.OnRetrievingCommentById";
    }
}
