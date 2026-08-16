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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial class ApprovalReviewService
    {
        private static void ValidateOnDismissApprovalReview(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        // Dismissal is the PUBLISHER-tier act, not the reviewer's. §8.8 dismisses a review as a
        // consequence of an entity-scoped change, and §9.5 makes dismissal something that
        // happens TO a review rather than a verdict its author declares — so a Reviewer, whose
        // instrument is the verdict on their own row, may never drive one to Dismissed by hand
        // (the same HR-3 shape the approve operations use). The check is deliberately the
        // publisher SUBSET of HasReviewRole: the global Publisher, an Admin, or any
        // entity-scoped "%EntityType%-Publisher"; the review roles are excluded.
        private static bool HasPublisherRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // Tier 1, row-local. Kept alongside the broker decision below rather than replaced by
        // it: §14.6 rule 2 and §8.6.1 make the coarse duplicate intentional — one role comparison
        // instead of a table read, and a defect in the gathering can only ever make the pair
        // stricter, never looser.
        private static void ValidateUserCanDismissApprovalReview(SecurityContext securityContext)
        {
            if (HasPublisherRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to dismiss this approval review.");
            }
        }

        // Tier 2, cross-entity — and the half that makes the gate mean what §8.9 rule 2 says.
        // HasPublisherRole above matches ANY "-Publisher" suffix, because a review row names no
        // entity type: a bare Tag-Publisher passes it for a ContentItem↔BibleReference
        // association's approval. The broker resolves the entity behind the approval — for an
        // association, both of its endpoints (§14.7 posture A′ rule 2) — so the tier is finally
        // checked against the thing actually under review.
        //
        // Asked about the STORED approval id, never a payload value: dismissal is decided against
        // the row as it is, and the request carries nothing but the review's own id anyway.
        private async ValueTask ValidateUserMayDismissApprovalReviewAsync(
            Guid approvalId,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            AccessVerdict verdict = await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId: approvalId,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason server-side, nothing about the policy to the caller.
                await this.loggingBroker.LogWarningAsync(
                    $"Approval review dismissal denied for approval {approvalId}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to dismiss this approval review.");
            }
        }

        // A dismissed review stays dismissed (§9.5): the row is retained as evidence, and a
        // second dismissal would re-stamp the audit values and re-publish the fact for a state
        // it is already in. Refused rather than treated as idempotent so the caller learns the
        // request was a no-op instead of it silently succeeding.
        private static void ValidateStorageApprovalReviewIsDismissable(
            ApprovalReview storageApprovalReview)
        {
            if (storageApprovalReview.StatusId == ApprovalStatus.Dismissed)
            {
                throw new InvalidApprovalReviewException(
                    message: "Approval review is already dismissed.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transition becomes a probe for which reviews used to exist.
        private static void ValidateStorageApprovalReviewIsNotDeleted(
            ApprovalReview storageApprovalReview,
            Guid approvalReviewId)
        {
            if (storageApprovalReview.IsDeleted)
            {
                throw new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");
            }
        }
    }
}
