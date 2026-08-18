// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;

namespace Glory2Him.Core.Models.Configurations
{
    internal static partial class EventBrokerIdentifiers
    {
        // Subscription identifiers only — the approval workflow owns no event address of its
        // own. It listens on addresses other services publish, and causes its writes through
        // those services' transition verbs (§10.17 rules 4-5), so there is nothing here to
        // register in the address map.
        //
        // The address each subscription binds to is the entity's TOP-LAYER fact, never the
        // foundation's, wherever a layer above the foundation exists (§10.17 rule 1). Today
        // that splits the seven approvable entities two ways, and the split is load-bearing:
        // ContentItem and Link publish their completion facts from their processing service,
        // so subscribing to the foundation instead would react to the version fork's
        // bookkeeping writes as well as the amendment (§10.17 rule 2). The other five have
        // nothing above their foundation, so the foundation fact IS the top-layer fact.
        //
        // No -Removed subscription for any of them. A takedown is not a moderation step and
        // must never re-open an approval (§9.7.6).

        public static readonly Guid ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId =
            new Guid("01a0e17c-4b12-754e-b0d4-1e2525193391");

        public const string ApprovalOrchestrationOnApprovalReviewAddedSubscriptionName =
            "ApprovalOrchestrationService.OnApprovalReviewAdded";

        public static readonly Guid ApprovalOrchestrationOnTagAddedSubscriptionId =
            new Guid("01a0e17c-3a01-743d-afc3-0d1414082280");

        public const string ApprovalOrchestrationOnTagAddedSubscriptionName =
            "ApprovalOrchestrationService.OnTagAdded";

        public static readonly Guid ApprovalOrchestrationOnTagModifiedSubscriptionId =
            new Guid("01a0e17c-3a02-7082-91b1-58e873758bae");

        public const string ApprovalOrchestrationOnTagModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnTagModified";

        public static readonly Guid ApprovalOrchestrationOnContentItemAddedSubscriptionId =
            new Guid("01a0e17c-3a03-713c-b4d0-3cc0e7c7067f");

        public const string ApprovalOrchestrationOnContentItemAddedSubscriptionName =
            "ApprovalOrchestrationService.OnContentItemAdded";

        public static readonly Guid ApprovalOrchestrationOnContentItemModifiedSubscriptionId =
            new Guid("01a0e17c-3a04-73a1-a60c-1f5b6e4c0daa");

        public const string ApprovalOrchestrationOnContentItemModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnContentItemModified";

        public static readonly Guid ApprovalOrchestrationOnLinkAddedSubscriptionId =
            new Guid("01a0e17c-3a05-7f64-87b0-631ba85fc46f");

        public const string ApprovalOrchestrationOnLinkAddedSubscriptionName =
            "ApprovalOrchestrationService.OnLinkAdded";

        public static readonly Guid ApprovalOrchestrationOnLinkModifiedSubscriptionId =
            new Guid("01a0e17c-3a06-7ce4-ab32-8a07ebb1078f");

        public const string ApprovalOrchestrationOnLinkModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnLinkModified";

        public static readonly Guid ApprovalOrchestrationOnCommentAddedSubscriptionId =
            new Guid("01a0e17c-3a07-7f0b-acbe-c282ffe73224");

        public const string ApprovalOrchestrationOnCommentAddedSubscriptionName =
            "ApprovalOrchestrationService.OnCommentAdded";

        public static readonly Guid ApprovalOrchestrationOnCommentModifiedSubscriptionId =
            new Guid("01a0e17c-3a08-76b6-b9b7-846150caa71a");

        public const string ApprovalOrchestrationOnCommentModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnCommentModified";

        public static readonly Guid ApprovalOrchestrationOnReactionAddedSubscriptionId =
            new Guid("01a0e17c-3a09-7a5b-a262-59fb81d5529b");

        public const string ApprovalOrchestrationOnReactionAddedSubscriptionName =
            "ApprovalOrchestrationService.OnReactionAdded";

        public static readonly Guid ApprovalOrchestrationOnReactionModifiedSubscriptionId =
            new Guid("01a0e17c-3a0a-7381-9fb3-53769a91e386");

        public const string ApprovalOrchestrationOnReactionModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnReactionModified";

        public static readonly Guid ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId =
            new Guid("01a0e17c-3a0b-77a1-a05f-3b7e5b17c847");

        public const string ApprovalOrchestrationOnBibleReferenceAddedSubscriptionName =
            "ApprovalOrchestrationService.OnBibleReferenceAdded";

        public static readonly Guid ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionId =
            new Guid("01a0e17c-3a0c-77ad-a587-d04611fa2d88");

        public const string ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnBibleReferenceModified";

        public static readonly Guid ApprovalOrchestrationOnAssociationAddedSubscriptionId =
            new Guid("01a0e17c-3a0d-7743-b56a-4555cd6fb2b5");

        public const string ApprovalOrchestrationOnAssociationAddedSubscriptionName =
            "ApprovalOrchestrationService.OnAssociationAdded";

        public static readonly Guid ApprovalOrchestrationOnAssociationModifiedSubscriptionId =
            new Guid("01a0e17c-3a0e-7c2c-8c98-aa741a648174");

        public const string ApprovalOrchestrationOnAssociationModifiedSubscriptionName =
            "ApprovalOrchestrationService.OnAssociationModified";
    }
}
