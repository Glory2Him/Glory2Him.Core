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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial class ApprovalCommentService
    {
        private static void ValidateOnResolveApprovalComment(Guid approvalCommentId) =>
            Validate(
                message: "Approval comment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalCommentId), Parameter: nameof(ApprovalComment.Id)));

        // Owner or Admin, and deliberately no wider. The review roles read a comment thread
        // (§14.1) without owning it, so a Reviewer seeing an unanswered question may answer it
        // in a comment of their own — declaring it answered is the author's call, or an
        // administrator's. Admin is the GLOBAL role only: an entity-scoped "-Publisher" is a
        // workflow role, and lifting the block
        // RequireReviewCommentResolutionBeforeApprovals holds shut is an administrative
        // override rather than part of deciding the approval.
        private async ValueTask ValidateUserCanResolveStorageApprovalCommentAsync(
            ApprovalComment storageApprovalComment,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalComment.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to resolve this approval comment.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transition becomes a probe for which comments used to exist.
        private static void ValidateStorageApprovalCommentIsNotDeleted(
            ApprovalComment storageApprovalComment,
            Guid approvalCommentId)
        {
            if (storageApprovalComment.IsDeleted)
            {
                throw new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");
            }
        }

        // A resolution that changes nothing is refused rather than treated as idempotent, so the
        // caller learns the request was a no-op instead of it silently re-stamping the audit
        // values and re-publishing the fact for a state the row is already in. That matters more
        // here than it would for a display flag: a spurious Resolved announces to anything
        // watching RequireReviewCommentResolutionBeforeApprovals that a gate moved when it did
        // not.
        private static void ValidateStorageApprovalCommentResolutionChanges(
            ApprovalComment storageApprovalComment,
            bool isResolved)
        {
            if (storageApprovalComment.IsResolved == isResolved)
            {
                throw new InvalidApprovalCommentException(
                    message: isResolved
                        ? "Approval comment is already resolved."
                        : "Approval comment is already unresolved.");
            }
        }
    }
}
