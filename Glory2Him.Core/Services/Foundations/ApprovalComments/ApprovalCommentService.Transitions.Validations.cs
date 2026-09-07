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

        // Owner or the PUBLISHER tier, and deliberately no wider. The review roles read a comment
        // thread (§14.1) without owning it, so a reviewer who wants to respond to an outstanding
        // comment writes one of their own — declaring somebody else's comment settled belongs to
        // the author, or to the tier the block actually stops.
        //
        // WHY THE PUBLISHER TIER RATHER THAN ADMINISTRATORS ALONE. An outstanding comment holds
        // the APPROVAL shut under RequireReviewCommentResolutionBeforeApprovals, and the people
        // that block stops are exactly the people who decide the approval — the publisher tier at
        // every §18.6 scope the entity composes. A reviewer is never held by the gate, which is
        // why the review suffix is not admitted here even though HasReviewRole above accepts it
        // for the reads.
        //
        // ROW-LOCAL, so it can only ask the §18.6 naming CONVENTION: a comment row does not say
        // which entity type its approval targets, so this cannot tell a Tag-Publishers holder
        // from a ContentItem-Publishers one. Matching the entity is IAccessBroker's half
        // (ValidateUserMayResolveApprovalCommentAsync), which reads the entity behind the
        // approval and composes the real names — and asks the ReadOnly veto against them. Both
        // run: §14.6 rule 2 makes the duplicate intentional, and two throwing gates compose to an
        // AND, so this one must not be NARROWER than the broker's or it would delete the widening.
        private async ValueTask ValidateUserCanResolveStorageApprovalCommentAsync(
            ApprovalComment storageApprovalComment,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApprovalComment.CreatedBy == actorUserId;

            if (isOwner is false && HasPublisherRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to resolve this approval comment.");
            }
        }

        // The publisher tier as a row-local check can see it: the two global names, and any
        // "{Entity}-Publishers" / "{Entity}-{ContentType}-Publishers" by the §18.6 capability-last
        // convention. Administrators is in the set because it clears every tier — dropping it here
        // would withdraw the route §14.7 rule 5 opened.
        private static bool HasPublisherRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Administrators)
                || securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(Roles.PublishersSuffix, StringComparison.Ordinal));

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
