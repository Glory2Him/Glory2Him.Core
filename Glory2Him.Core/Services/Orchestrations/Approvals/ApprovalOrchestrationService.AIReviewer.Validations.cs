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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        private static void ValidateOnRetrieveAIReviewerStatus(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        private static void ValidateOnRequestAIReviewer(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        private static void ValidateOnWithdrawAIReviewer(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval orchestration request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(EntityType)),
                (Rule: IsInvalid(entityId), Parameter: "EntityId"));

        // Fail-closed (§8.4 rule 2): the resolved policy must say so explicitly, on every write,
        // never assumed from whatever the picker last showed the caller.
        private static void ValidateAIReviewerIsOffered(
            ApprovalReviewerScope scope,
            EntityType entityType,
            Guid entityId)
        {
            if (scope.IsAIReviewerOffered is false)
            {
                throw new InvalidApprovalOrchestrationException(
                    message: $"The AI reviewer is not offered for {entityType} with id: "
                        + $"{entityId}.");
            }
        }
    }
}
