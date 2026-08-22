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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial class ApprovalReviewService
    {
        private static void ValidateOnDismissApprovalReview(Guid approvalReviewId) =>
            Validate(
                message: "Approval review is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewId), Parameter: nameof(ApprovalReview.Id)));

        // Dismissal belongs to the approval workflow alone (§7.7 rule 7, #295). A reviewer
        // records Approved or Rejected; Dismissed is what happens TO a verdict when the content
        // it judged has changed, and that is not a decision any person makes.
        //
        // This replaced a two-tier publisher check. The tiers existed because a person could
        // once reach this verb through a public API route and a public event address; both are
        // gone, so the question is no longer "which people may dismiss" but "is this the
        // workflow at all".
        //
        // Unreachable in practice, and deliberately kept — but narrower than it looks. The one
        // public seam, DismissStaleApprovalReviewAsync, calls CreateSystemAsync itself before
        // delegating, so anything entering THERE mints a passing context by construction and
        // can never fail this. What it actually guards is a future second caller of the private
        // DoDismissApprovalReviewAsync that supplies its own envelope — which is exactly how
        // both of the routes this commit removed used to arrive.
        private static void ValidateDismissalIsTheWorkflowsOwnAct(SecurityContext securityContext)
        {
            if (securityContext.IsSystemIdentity is false)
            {
                throw new UnauthorizedApprovalReviewException(
                    message: "Dismissal is the approval workflow's own act; "
                        + "no user may perform it.");
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
