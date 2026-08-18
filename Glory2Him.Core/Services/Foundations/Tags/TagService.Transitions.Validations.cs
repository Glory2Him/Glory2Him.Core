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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial class TagService
    {
        private static void ValidateOnSubmitTag(Guid tagId) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tagId), Parameter: nameof(Tag.Id)));

        private static void ValidateOnTransitionTagApproval(Tag tag) =>
            Validate(
                message: "Tag is invalid, fix the errors and try again.",
                (Rule: IsInvalid(tag.Id), Parameter: nameof(Tag.Id)),

                // This operation owns the whole of IApproval, so it is the one allowed to carry
                // these — but only to a state the approval workflow can hold a row in. Draft is
                // refused because a row reaches it once, at creation, and submitting is its own
                // verb; Dismissed belongs to a later withdrawal step. Submitted is admitted, and
                // is what an override re-opens a terminal row to.
                (Rule: IsNotAnApprovalTransitionTarget(tag.ApprovalStatus),
                    Parameter: nameof(Tag.ApprovalStatus)),

                // publication is a consequence of approval — a row cannot be published while
                // being rejected, and a publish date without publication is a date nothing
                // reads
                (Rule: IsPublishedWithoutApproval(
                        tag.ApprovalStatus, tag.IsPublished),
                    Parameter: nameof(Tag.IsPublished)),

                (Rule: IsPublishDateWithoutPublication(
                        tag.IsPublished, tag.PublishDate),
                    Parameter: nameof(Tag.PublishDate)),

                // There is no such thing as a bypass-reject or a bypass-reopen: a waiver waives
                // the §8.5 approval conditions, and nothing is being waived when approval is not
                // what is being granted (§9.7.5). Admitting one would stamp IsApprovedByBypass
                // on a rejection.
                (Rule: IsBypassWithoutApproval(
                        tag.IsApprovedByBypass, tag.ApprovalStatus),
                    Parameter: nameof(Tag.IsApprovedByBypass)),

                // A bypass is only tolerable because it leaves a record, and an unexplained one
                // records nothing worth reading. Validated HERE — before the gate reads any
                // policy — so an unexplained bypass is refused under every policy, including one
                // that would have permitted the waiver.
                (Rule: IsMissingBypassReason(
                        tag.IsApprovedByBypass, tag.ApprovedByBypassReason),
                    Parameter: nameof(Tag.ApprovedByBypassReason)),

                // The column this lands in is nvarchar(500). Without the bound, the same payload
                // comes back from SQL Server as a "contact support" dependency failure naming no
                // field at all.
                (Rule: IsGreaterThan(tag.ApprovedByBypassReason, 500),
                    Parameter: nameof(Tag.ApprovedByBypassReason)));

        // Submitting is the owner-or-publisher act of §9.2. It is deliberately the SAME set the
        // modify carve-out admits (design §9.2 rules 4-6): a dedicated status-only verb must
        // not be narrower than the identical transition reached through a content edit. A
        // Reviewer is absent by design — HasPublisherRole excludes the review tier (§8.6 HR-3),
        // and a Reviewer moves an outcome only through the approval workflow, never by hand.
        private async ValueTask ValidateUserCanSubmitStorageTagAsync(
            Tag storageTag,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageTag.CreatedBy == actorUserId;

            bool isPermitted =
                isOwner
                    || HasPublisherRole(securityContext);

            if (isPermitted is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to submit this tag.");
            }
        }

        // Approving is the PUBLISHER-tier decision, and it is the narrowest gate in the service
        // because this is the only path by which a tag becomes publicly visible.
        //
        // Two hard rules meet here (design §8.6):
        //
        // HR-3 — a Reviewer may NEVER set an approval status. A reviewer's instrument is the
        // ApprovalReview record; they move the outcome only indirectly, through automatic
        // approval. HasPublisherRole is strictly narrower than the review tier and excludes the
        // Reviewer roles for exactly this reason.
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
        private async ValueTask<bool> ValidateUserCanTransitionStorageTagApprovalAsync(
            Tag storageTag,
            Tag tag,
            SecurityContext securityContext,
            bool isSystemIdentity,
            CancellationToken cancellationToken)
        {
            // Resolved from the STORED status, never the caller's copy — the same reason the
            // author is. A caller-supplied status would be self-certification: anyone could
            // present an approved row as Submitted and decide it as an ordinary round, which is
            // the entire gate.
            bool isOverride =
                storageTag.ApprovalStatus == ApprovalStatus.Approved
                    || storageTag.ApprovalStatus == ApprovalStatus.Rejected;

            // §8.6 HR-4. Moving a row OUT of a terminal state is an override, and it is what
            // keeps "terminal" meaningful: a state that the owner or a Publisher could edit out
            // of would not be terminal at all (§3.4 rules 7, 16). It is gated to Admin — and to
            // the workflow, below — and to nobody else.
            //
            // Run row-local and FIRST, so an unauthorised override costs one role comparison
            // rather than several table reads, and so a defect in the access decision's
            // gathering can only ever make this stricter (§8.6.1).
            if (isOverride
                && isSystemIdentity is false
                && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to transition this tag.");
            }

            // The workflow's own writes have no human permitted to make them, which is why a
            // second admissible actor exists at all (§8.6 regardless-rule 1): the reviewer whose
            // own review fires an automatic approval is the one party barred from deciding it,
            // and the previously published sibling a newly approved version demotes is itself
            // Approved, so no Publisher may touch it either.
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
                return tag.IsApprovedByBypass;
            }

            if (HasPublisherRole(securityContext) is false)
            {
                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to approve this tag.");
            }

            // Re-opening a row to Submitted decides nothing — it returns the row to review
            // rather than granting or withholding approval — so there is no approval decision to
            // ask for, and ApprovalDecision has no member that would honestly express one. The
            // Admin gate above is the whole authority for it.
            if (tag.ApprovalStatus == ApprovalStatus.Submitted)
            {
                return false;
            }

            AccessVerdict verdict = await this.accessBroker.MayDecideApprovalAsync(
                new ApprovalDecisionQuery
                {
                    EntityType = EntityType.Tag,
                    EntityId = storageTag.Id,

                    // A tag carries no content type, so its policy tier is (Tag, null) — the
                    // same shape an association uses. There is exactly one tier to resolve
                    // against.
                    ContentType = null,

                    // One subject: the tag authorises from itself, keyed by its own type with
                    // no content type.
                    RoleSubjects = new List<RoleSubject>
                    {
                        new RoleSubject
                        {
                            EntityType = EntityType.Tag.ToString(),
                            ContentType = null,
                        },
                    },

                    // From STORAGE. Taking the author from the caller's copy would let a
                    // contributor name someone else as author and approve their own row.
                    EntityCreatedBy = storageTag.CreatedBy,

                    // A tag has no confidence score — that is an association's input. The
                    // decision engine treats a null score as "no score to weigh".
                    ConfidenceScore = null,

                    Decision = tag.ApprovalStatus == ApprovalStatus.Rejected
                        ? ApprovalDecision.Reject
                        : ApprovalDecision.Approve,

                    // The bypass REQUEST, which is all the caller's pair ever is. What lands on
                    // the row comes back on the verdict: asking here and writing from the answer
                    // is what stops a genuine waiver being un-recorded by the party it is
                    // evidence about. DoNotAllowBypassingSettings is resolved inside the
                    // decision and closes this route to everyone, Admin included.
                    IsBypassRequested = tag.IsApprovedByBypass,
                    BypassReason = tag.ApprovedByBypassReason,

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
                    $"Tag approval denied for {storageTag.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedTagException(
                    message: "The current user is not allowed to approve this tag.");
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
        private static void ValidateStorageTagIsTransitionable(
            Tag storageTag)
        {
            bool isTransitionable =
                storageTag.ApprovalStatus == ApprovalStatus.Submitted
                    || storageTag.ApprovalStatus == ApprovalStatus.Approved
                    || storageTag.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTransitionable is false)
            {
                throw new InvalidTagException(
                    message: "Tag cannot be approved from status " +
                        $"{storageTag.ApprovalStatus}.");
            }
        }

        // Only a Draft may be submitted. A row already Submitted, Approved, Rejected or
        // Dismissed is not a fresh submission, and re-submitting one would either re-open a
        // decided item or re-announce a pending one (design §9.7.1, issue #111 case 7).
        private static void ValidateStorageTagIsSubmittable(
            Tag storageTag)
        {
            if (storageTag.ApprovalStatus != ApprovalStatus.Draft)
            {
                throw new InvalidTagException(
                    message: "Tag cannot be submitted from status " +
                        $"{storageTag.ApprovalStatus}.");
            }
        }

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transitions become a probe for which tags used to exist.
        private static void ValidateStorageTagIsNotDeleted(
            Tag storageTag,
            Guid tagId)
        {
            if (storageTag.IsDeleted)
            {
                throw new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");
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

                Message = "Bypass requires an approved tag."
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
                Message = "Is published requires an approved tag."
            };

        private static dynamic IsPublishDateWithoutPublication(
            bool isPublished,
            DateTimeOffset? publishDate) => new
            {
                Condition = isPublished is false && publishDate.HasValue,
                Message = "Publish date requires a published tag."
            };
    }
}
