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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        private static void ValidateOnSubmitContentItem(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateOnFindHighestVersionInGroup(Guid groupId) =>
            Validate(
                message: "ContentItem is invalid, fix the errors and try again.",
                (Rule: IsInvalid(groupId), Parameter: nameof(ContentItem.GroupId)));

        private static void ValidateOnFindPublishedContentItemByGroup(Guid groupId) =>
            Validate(
                message: "ContentItem is invalid, fix the errors and try again.",
                (Rule: IsInvalid(groupId), Parameter: nameof(ContentItem.GroupId)));

        private static void ValidateOnUnpublishContentItem(Guid contentItemId) =>
            Validate(
                message: "ContentItem is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        // Administrators or the workflow, and NOT the publisher tier. The row being
        // unpublished is itself Approved, and §8.6 HR-4 bars a publisher from moving
        // an approved row — the same reason the override is Administrators-gated. The system
        // identity is admissible because it arrived on a verified envelope.
        private static void ValidateUserCanUnpublishContentItem(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not authenticated.");
            }

            bool isPermitted =
                securityContext.IsSystemIdentity
                    || securityContext.Roles.Contains(Roles.Administrators);

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to unpublish this "
                        + "contentItem.");
            }
        }

        // Unpublishing is a WRITE, so the veto covers it at all three tiers (§18.6 rule 2).
        // Without this it was the one content-item write path that asked no block role at all —
        // and it is the one that takes published content off the site, which is exactly the act
        // a sanction exists to stop. The same argument the hard remove is gated on: a block that
        // stops the reversible act and not this one is the wrong way round.
        //
        // Asked against the STORED row, and skipped entirely for the system identity — that is
        // the publication swap moving an incumbent aside (§9.7.7 rule 7), which holds no roles by
        // construction and is not the party this refuses.
        private static void ValidateUserIsNotBlockedFromUnpublishing(
            SecurityContext securityContext,
            ContentItem storageContentItem)
        {
            if (securityContext.IsSystemIdentity)
            {
                return;
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ContentItemReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is blocked from contributing content items.");
            }

            ValidateUserIsNotBlockedFromContentType(
                securityContext,
                storageContentItem.ContentType);
        }

        private static void ValidateOnTransitionContentItemApproval(ContentItem contentItem) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),

                // This operation owns the whole of IApproval, so it is the one allowed to carry
                // these — but only to a state the approval workflow can hold a row in. Draft is
                // refused because a row reaches it once, at creation, and submitting is its own
                // verb; Dismissed belongs to a later withdrawal step. Submitted is admitted, and
                // is what an override re-opens a terminal row to.
                (Rule: IsNotAnApprovalTransitionTarget(contentItem.ApprovalStatus),
                    Parameter: nameof(ContentItem.ApprovalStatus)),

                // publication is a consequence of approval — a row cannot be published while
                // being rejected, and a publish date without publication is a date nothing
                // reads
                (Rule: IsPublishedWithoutApproval(
                        contentItem.ApprovalStatus, contentItem.IsPublished),
                    Parameter: nameof(ContentItem.IsPublished)),

                (Rule: IsPublishDateWithoutPublication(
                        contentItem.IsPublished, contentItem.PublishDate),
                    Parameter: nameof(ContentItem.PublishDate)),

                // There is no such thing as a bypass-reject or a bypass-reopen: a waiver waives
                // the §8.5 approval conditions, and nothing is being waived when approval is not
                // what is being granted (§9.7.5). Admitting one would stamp IsApprovedByBypass
                // on a rejection.
                (Rule: IsBypassWithoutApproval(
                        contentItem.IsApprovedByBypass, contentItem.ApprovalStatus),
                    Parameter: nameof(ContentItem.IsApprovedByBypass)),

                // A bypass is only tolerable because it leaves a record, and an unexplained one
                // records nothing worth reading. Validated HERE — before the gate reads any
                // policy — so an unexplained bypass is refused under every policy, including one
                // that would have permitted the waiver.
                (Rule: IsMissingBypassReason(
                        contentItem.IsApprovedByBypass, contentItem.ApprovedByBypassReason),
                    Parameter: nameof(ContentItem.ApprovedByBypassReason)),

                // The column this lands in is nvarchar(500). Without the bound, the same payload
                // comes back from SQL Server as a "contact support" dependency failure naming no
                // field at all.
                (Rule: IsGreaterThan(contentItem.ApprovedByBypassReason, 500),
                    Parameter: nameof(ContentItem.ApprovedByBypassReason)));

        // Submitting is the owner-or-publisher act of §9.2. It is deliberately the SAME set the
        // modify carve-out admits (design §9.2 rules 4-6): a dedicated status-only verb must
        // not be narrower than the identical transition reached through a content edit. The
        // Reviewers tier is absent by design — HasPublisherRole excludes it (§8.6 HR-3),
        // and a reviewer moves an outcome only through the approval workflow, never by hand.
        private async ValueTask ValidateUserCanSubmitStorageContentItemAsync(
            ContentItem storageContentItem,
            SecurityContext securityContext)
        {
            // Submitting is a write, so the veto is asked of the stored type first — ahead of
            // the owner branch and of the publisher tier alike (§18.6 rule 2).
            ValidateUserIsNotBlockedFromContentType(
                securityContext,
                storageContentItem.ContentType);

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageContentItem.CreatedBy == actorUserId;

            bool isPermitted =
                isOwner
                    || HasPublisherRole(securityContext, storageContentItem.ContentType);

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to submit this content item.");
            }
        }

        // Approving is the PUBLISHER-tier decision, and it is the narrowest gate in the service
        // because this is the only path by which a content item becomes publicly visible.
        //
        // Two hard rules meet here (design §8.6):
        //
        // HR-3 — a reviewer may NEVER set an approval status. A reviewer's instrument is the
        // ApprovalReview record; they move the outcome only indirectly, through automatic
        // approval. HasPublisherRole is strictly narrower than the review tier and excludes the
        // Reviewers roles for exactly this reason.
        //
        // HR-2 — no one approves their own content unless AllowSelfApproval permits it. That
        // setting lives on another entity, so the question goes to IAccessBroker.
        //
        // Together they are what stop a contributor walking the whole path alone: create,
        // submit, approve, publish.
        //
        // The row-local publisher check below is kept even though the access decision repeats
        // it. It is not redundancy for its own sake: it is what makes an unauthorised caller
        // cost one role comparison instead of several table reads, and it means a defect in the
        // gathering can only ever make this gate stricter, never open it.
        //
        // Returns whether a bypass was USED rather than the whole verdict, because that single
        // bit is the whole of what the caller writes back. It is one of the two IApproval
        // members this operation DERIVES instead of accepting: they exist to record that the
        // conditions were waived, and a caller able to set them is equally able to clear them,
        // erasing the one event they are here to capture (design §9.7.1 rule 3). Two of the
        // paths below take no approval decision at all and so have no verdict to return;
        // fabricating one would invent a denial reason and an explanation nothing decided.
        private async ValueTask<bool> ValidateUserCanTransitionStorageContentItemApprovalAsync(
            ContentItem storageContentItem,
            ContentItem contentItem,
            SecurityContext securityContext,
            bool isSystemIdentity,
            CancellationToken cancellationToken)
        {
            // The veto, before every grant this method goes on to weigh — the publisher tier,
            // the Administrators override, and the access decision below them all. A block on
            // this content type refuses the transition however wide the caller's role
            // (§18.6 rule 2). The workflow's own system identity carries no roles, so it is
            // never the party this refuses.
            ValidateUserIsNotBlockedFromContentType(
                securityContext,
                storageContentItem.ContentType);

            // Resolved from the STORED status, never the caller's copy — the same reason the
            // author and the content type are. A caller-supplied status would be
            // self-certification: anyone could present an approved row as Submitted and decide
            // it as an ordinary round, which is the entire gate.
            bool isOverride =
                storageContentItem.ApprovalStatus == ApprovalStatus.Approved
                    || storageContentItem.ApprovalStatus == ApprovalStatus.Rejected;

            // §8.6 HR-4. Moving a row OUT of a terminal state is an override, and it is what
            // keeps "terminal" meaningful: a state that the owner or a publisher could edit out
            // of would not be terminal at all (§3.4 rules 7, 16). It is gated to Administrators — and to
            // the workflow, below — and to nobody else.
            //
            // Run row-local and FIRST, so an unauthorised override costs one role comparison
            // rather than several table reads, and so a defect in the access decision's
            // gathering can only ever make this stricter (§8.6.1).
            if (isOverride
                && isSystemIdentity is false
                && securityContext.Roles.Contains(Roles.Administrators) is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to transition this content item.");
            }

            // The workflow's own writes have no human permitted to make them, which is why a
            // second admissible actor exists at all (§8.6 regardless-rule 1): the reviewer whose
            // own review fires an automatic approval is the one party barred from deciding it,
            // and the previously published sibling a newly approved version demotes is itself
            // Approved, so no Publishers may touch it either.
            //
            // The bypass pair is CARRIED, not decided. The workflow reaches here as the
            // messenger of a decision a human already made and was authorised for on the
            // Approval row, and re-deriving it would answer a question this actor was never
            // asked — writing "no bypass" over a waiver the approval records, diverging the two
            // records (§9.8) and erasing exactly the evidence §9.7.1 rule 3 exists to keep.
            //
            // Nothing unexplained gets through on this route: the shape validation refuses a
            // bypass with no reason, and one paired with any target but Approved, before any
            // policy is read. And the claim reached here only on a verified envelope, which is
            // what establishes it was minted by this system (§16.7.1).
            if (isSystemIdentity)
            {
                return contentItem.IsApprovedByBypass;
            }

            if (HasPublisherRole(securityContext, storageContentItem.ContentType) is false)
            {
                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to approve this content item.");
            }

            // Re-opening a row to Submitted decides nothing — it returns the row to review
            // rather than granting or withholding approval — so there is no approval decision to
            // ask for, and ApprovalDecision has no member that would honestly express one. The
            // Administrators gate above is the whole authority for it.
            if (contentItem.ApprovalStatus == ApprovalStatus.Submitted)
            {
                return false;
            }

            AccessVerdict verdict = await this.accessBroker.MayDecideApprovalAsync(
                new ApprovalDecisionQuery
                {
                    EntityType = EntityType.ContentItem,
                    EntityId = storageContentItem.Id,

                    // A content item's policy tier is keyed by its content type (§18.6): the
                    // narrow (ContentItem, Story) publisher and the broad (ContentItem, null)
                    // one are different grants, and the row carries exactly one content type to
                    // resolve them against.
                    ContentType = storageContentItem.ContentType,

                    // One subject: the item authorises from itself, keyed by its own type and
                    // content type — unlike an association, which authorises from two endpoints.
                    RoleSubjects = new List<RoleSubject>
                    {
                        new RoleSubject
                        {
                            EntityType = EntityType.ContentItem.ToString(),
                            ContentType = storageContentItem.ContentType.ToString(),
                        },
                    },

                    // From STORAGE. Taking the author from the caller's copy would let a
                    // contributor name someone else as author and approve their own row.
                    EntityCreatedBy = storageContentItem.CreatedBy,

                    // A content item has no confidence score — that is an association's input.
                    // The decision engine treats a null score as "no score to weigh".
                    ConfidenceScore = null,

                    Decision = contentItem.ApprovalStatus == ApprovalStatus.Rejected
                        ? ApprovalDecision.Reject
                        : ApprovalDecision.Approve,

                    // The bypass REQUEST, which is all the caller's pair ever is. What lands on
                    // the row comes back on the verdict: asking here and writing from the answer
                    // is what stops a genuine waiver being un-recorded by the party it is
                    // evidence about. DoNotAllowBypassingSettings is resolved inside the
                    // decision and closes this route to everyone, Administrators included.
                    IsBypassRequested = contentItem.IsApprovedByBypass,
                    BypassReason = contentItem.ApprovedByBypassReason,

                    SecurityContext = securityContext,
                },
                cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason is logged server-side and the caller is told nothing
                // about the policy. The verdict's explanation names resolved settings — how
                // many approvals were required, which block fired — and exception messages and
                // their Data surface outward through a public event address.
                await this.loggingBroker.LogWarningAsync(
                    $"Content item approval denied for {storageContentItem.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedContentItemException(
                    message: "The current user is not allowed to approve this content item.");
            }

            return verdict.IsBypassUsed;
        }

        // What a row may be transitioned FROM. Draft is refused because a row reaches it once,
        // at creation, and submitting it is its own verb — deciding one would skip the
        // submission the workflow is built around. Dismissed is refused because a withdrawn row
        // is not in a round at all.
        //
        // Approved and Rejected ARE admitted here: they are terminal, but terminal means the
        // content is immutable and the way out is narrow, not that the row is unreachable. The
        // override gate is what decides who may act on one, and it has already run.
        private static void ValidateStorageContentItemIsTransitionable(
            ContentItem storageContentItem)
        {
            bool isTransitionable =
                storageContentItem.ApprovalStatus == ApprovalStatus.Submitted
                    || storageContentItem.ApprovalStatus == ApprovalStatus.Approved
                    || storageContentItem.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTransitionable is false)
            {
                throw new InvalidContentItemException(
                    message: "Content item cannot be approved from status " +
                        $"{storageContentItem.ApprovalStatus}.");
            }
        }

        // Only a Draft may be submitted. A row already Submitted, Approved, Rejected or
        // Dismissed is not a fresh submission, and re-submitting one would either re-open a
        // decided item or re-announce a pending one (design §9.7.1, issue #111 case 7).
        private static void ValidateStorageContentItemIsSubmittable(
            ContentItem storageContentItem)
        {
            if (storageContentItem.ApprovalStatus != ApprovalStatus.Draft)
            {
                throw new InvalidContentItemException(
                    message: "Content item cannot be submitted from status " +
                        $"{storageContentItem.ApprovalStatus}.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transitions become a probe for which content items used to exist.
        private static void ValidateStorageContentItemIsNotDeleted(
            ContentItem storageContentItem,
            Guid contentItemId)
        {
            if (storageContentItem.IsDeleted)
            {
                throw new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");
            }
        }

        private static dynamic IsNotAnApprovalTransitionTarget(
            ApprovalStatus approvalStatus) => new
            {
                Condition =
                    approvalStatus != ApprovalStatus.Approved
                        && approvalStatus != ApprovalStatus.Rejected
                        && approvalStatus != ApprovalStatus.Submitted,

                Message = "Approval status must be Submitted, Approved or Rejected."
            };

        // A waiver waives the §8.5 APPROVAL conditions. Rejecting withholds approval rather than
        // granting it and re-opening decides nothing at all, so neither has anything to waive
        // (§9.7.5). Refusing the pairing here keeps IsApprovedByBypass off any row that was not
        // approved.
        private static dynamic IsBypassWithoutApproval(
            bool isApprovedByBypass,
            ApprovalStatus approvalStatus) => new
            {
                Condition =
                    isApprovedByBypass
                        && approvalStatus != ApprovalStatus.Approved,

                Message = "Bypass requires an approved content item."
            };

        private static dynamic IsMissingBypassReason(
            bool isApprovedByBypass,
            string? approvedByBypassReason) => new
            {
                Condition =
                    isApprovedByBypass
                        && string.IsNullOrWhiteSpace(approvedByBypassReason),

                Message = "Bypass reason is required when bypassing."
            };

        private static dynamic IsPublishedWithoutApproval(
            ApprovalStatus approvalStatus,
            bool isPublished) => new
            {
                Condition = isPublished && approvalStatus != ApprovalStatus.Approved,
                Message = "Is published requires an approved content item."
            };

        private static dynamic IsPublishDateWithoutPublication(
            bool isPublished,
            DateTimeOffset? publishDate) => new
            {
                Condition = isPublished is false && publishDate.HasValue,
                Message = "Publish date requires a published content item."
            };
    }
}
