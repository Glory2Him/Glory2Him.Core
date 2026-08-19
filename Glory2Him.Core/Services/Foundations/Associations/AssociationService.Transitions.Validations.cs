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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        private static void ValidateAnchorAssociationIsNotNull(Association anchorAssociation)
        {
            if (anchorAssociation is null)
            {
                throw new NullAssociationException(
                    message: "Content item association anchor is null.");
            }
        }

        private static void ValidateOnTransitionAssociationApproval(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.Id), Parameter: nameof(Association.Id)),

                // This operation owns the whole of IApproval, so it is the one allowed to carry
                // these — but only to a state the approval workflow can hold a row in. Draft is
                // refused because a row reaches it once, at creation; Dismissed belongs to a
                // later withdrawal step. Submitted is admitted, and is what an override re-opens
                // a terminal row to.
                (Rule: IsNotAnApprovalTransitionTarget(association.ApprovalStatus),
                    Parameter: nameof(Association.ApprovalStatus)),

                // publication is a consequence of approval — a row cannot be published while
                // being rejected, and a publish date without publication is a date nothing
                // reads
                (Rule: IsPublishedWithoutApproval(
                        association.ApprovalStatus, association.IsPublished),
                    Parameter: nameof(Association.IsPublished)),

                (Rule: IsPublishDateWithoutPublication(
                        association.IsPublished, association.PublishDate),
                    Parameter: nameof(Association.PublishDate)),

                // NARROWER than the transition itself, which admits three targets. There is no
                // such thing as a bypass-reject or a bypass-reopen: a rejection withholds
                // approval rather than granting it and re-opening decides nothing, so neither
                // has anything to waive, DoNotAllowBypassingSettings does not gate them and
                // IsApprovedByBypass stays false (§9.7.5).
                //
                // Admitting one would go wrong three ways at once: the row would be stamped
                // IsApprovedByBypass on a REJECTION, the access decision would have been taken
                // out for Decision = Approve, and the fact published would be Approved —
                // telling every subscriber the opposite of what happened.
                (Rule: IsBypassWithoutApproval(
                        association.IsApprovedByBypass, association.ApprovalStatus),
                    Parameter: nameof(Association.IsApprovedByBypass)),

                // A bypass is only tolerable because it leaves a record, and an unexplained one
                // records nothing worth reading. Validated HERE — before the gate reads any
                // policy — so an unexplained bypass is refused under every policy, including one
                // that would have permitted the waiver.
                (Rule: IsMissingBypassReason(
                        association.IsApprovedByBypass, association.ApprovedByBypassReason),
                    Parameter: nameof(Association.ApprovedByBypassReason)),

                // The column this lands in is nvarchar(500). Without the bound, the same
                // payload comes back from SQL Server as a "contact support" dependency
                // failure naming no field at all — the same reasoning as the two string
                // columns set-confidence owns.
                (Rule: IsGreaterThan(association.ApprovedByBypassReason, 500),
                    Parameter: nameof(Association.ApprovedByBypassReason)));

        private static void ValidateOnSortAssociation(
            Association association,
            Association anchorAssociation,
            SortPosition position) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.Id), Parameter: nameof(Association.Id)),
                (Rule: IsInvalidAnchorId(anchorAssociation.Id), Parameter: "AnchorAssociationId"),

                // positioning a row relative to itself is a no-op the caller did not mean
                (Rule: IsSameAssociation(association.Id, anchorAssociation.Id),
                    Parameter: "AnchorAssociationId"),

                (Rule: IsInvalid(position), Parameter: nameof(SortPosition)));

        private static void ValidateOnSetAssociationConfidence(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.Id), Parameter: nameof(Association.Id)),

                (Rule: IsOutOfRange(association.ConfidenceScore),
                    Parameter: nameof(Association.ConfidenceScore)),

                // provenance travels with the score. A score attributed to a model with no
                // batch behind it cannot be retracted by a sweep over that batch, and a batch
                // with no score is provenance for nothing.
                (Rule: IsProvenanceIncomplete(
                        association.SourceBatchId, association.ModelVersion),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsProvenanceWithoutScore(
                        association.ConfidenceScore,
                        association.SourceBatchId,
                        association.ModelVersion),
                    Parameter: nameof(Association.SourceBatchId)),

                // the two string columns this operation owns. Add and modify both bound them;
                // without these the same payload that add rejects cleanly comes back from here
                // as a "contact support" dependency failure raised by SQL Server, naming no
                // field at all.
                (Rule: IsGreaterThan(association.ConfidenceReason, 500),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsGreaterThan(association.ModelVersion, 128),
                    Parameter: nameof(Association.ModelVersion)));

        private static void ValidateOnSetAssociationScope(
            Guid associationId,
            Scope? entityAScope,
            Scope? entityBScope) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)),

                // a call that names neither endpoint is not a scope change at all
                (Rule: IsNoScopeSupplied(entityAScope, entityBScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsInvalid(entityAScope), Parameter: nameof(Association.EntityAScope)),
                (Rule: IsInvalid(entityBScope), Parameter: nameof(Association.EntityBScope)));

        // Approving is the PUBLISHER-tier decision, and it is the narrowest gate in the
        // service because this is the only path by which an association becomes publicly
        // visible.
        //
        // Two hard rules meet here (design §8.6):
        //
        // HR-3 — a Reviewer may NEVER set an approval status. A reviewer's instrument is the
        // ApprovalReview record; they move the outcome only indirectly, through automatic
        // approval. %EntityType%-Reviewer is not a weaker %EntityType%-Publisher, it is a
        // different job, so the review-role helper is deliberately NOT used here.
        //
        // HR-2 — no one approves their own content unless AllowSelfApproval permits it. That
        // setting lives on another entity, so the question goes to IAccessBroker.
        //
        // Together they are what stop a contributor walking the whole path alone: create,
        // submit, approve, publish.
        //
        // The row-local publisher check below is kept even though the access decision repeats
        // it. It is not redundancy for its own sake: it is what makes an unauthorised caller
        // cost one role comparison instead of four table reads, and it means a defect in the
        // gathering can only ever make this gate stricter, never open it.
        // Returns the verdict rather than only throwing on refusal, because the caller has to
        // write IsApprovedByBypass from it, and the bypass log line needs what the waiver
        // covered. Those two IApproval members are the one part of the interface this operation
        // DERIVES instead of accepting: they exist to record that the conditions were waived,
        // and a caller able to set them is equally able to clear them, erasing the one event
        // they are here to capture (design §9.7.1 rule 3).
        private async ValueTask<AccessVerdict> ValidateUserCanTransitionStorageAssociationApprovalAsync(
            Association storageAssociation,
            Association association,
            SecurityContext securityContext,
            bool isSystemIdentity,
            CancellationToken cancellationToken)
        {
            // Resolved from the STORED status, never the caller's copy — the same reason the
            // endpoints are. A caller-supplied status would be self-certification: anyone could
            // present an approved row as Submitted and decide it as an ordinary round, which is
            // the entire gate.
            bool isOverride =
                storageAssociation.ApprovalStatus == ApprovalStatus.Approved
                    || storageAssociation.ApprovalStatus == ApprovalStatus.Rejected;

            // §8.6 HR-4. Moving a row OUT of a terminal state is an override, and it is what
            // keeps "terminal" meaningful: a state that the owner or a Publisher could edit out
            // of would not be terminal at all (§3.4 rules 7, 16). It is gated to Admin — and to
            // the workflow, below — and to nobody else.
            //
            // Run row-local and FIRST, so an unauthorised override costs one role comparison
            // rather than four table reads, and so a defect in the access decision's gathering
            // can only ever make this stricter (§8.6.1).
            if (isOverride
                && isSystemIdentity is false
                && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to transition " +
                        "this content item association.");
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
                return CarriedBypassVerdict(association.IsApprovedByBypass);
            }

            if (HasPublisherRoleForAssociation(securityContext, storageAssociation) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");
            }

            // Re-opening a row to Submitted decides nothing — it returns the row to review
            // rather than granting or withholding approval — so there is no approval decision to
            // ask for, and ApprovalDecision has no member that would honestly express one. The
            // Admin gate above is the whole authority for it.
            if (association.ApprovalStatus == ApprovalStatus.Submitted)
            {
                return NoDecisionVerdict();
            }

            AccessVerdict verdict = await this.accessBroker.MayDecideApprovalAsync(
                new ApprovalDecisionQuery
                {
                    EntityType = EntityType.Association,
                    EntityId = storageAssociation.Id,

                    // An association's own policy tier is (Association, null). Its endpoints'
                    // content types authorise the CALLER, they do not key the policy — and a
                    // Testimony-to-Devotional row would make the narrow tier ambiguous anyway,
                    // because neither endpoint is more specific than the other.
                    ContentType = null,

                    // Both endpoints, because an association is authorised from them rather
                    // than from itself, and one is enough.
                    RoleSubjects = new List<RoleSubject>
                    {
                        new RoleSubject
                        {
                            EntityType = storageAssociation.EntityAType.ToString(),
                            ContentType = storageAssociation.EntityAContentType?.ToString(),
                        },
                        new RoleSubject
                        {
                            EntityType = storageAssociation.EntityBType.ToString(),
                            ContentType = storageAssociation.EntityBContentType?.ToString(),
                        },
                    },

                    // From STORAGE. Taking the author from the caller's copy would let a
                    // contributor name someone else as author and approve their own row.
                    EntityCreatedBy = storageAssociation.CreatedBy,
                    ConfidenceScore = storageAssociation.ConfidenceScore,

                    Decision = association.ApprovalStatus == ApprovalStatus.Rejected
                        ? ApprovalDecision.Reject
                        : ApprovalDecision.Approve,

                    // The bypass REQUEST, which is all the caller's pair ever is. What lands on
                    // the row comes back on the verdict: asking here and writing from the answer
                    // is what stops a genuine waiver being un-recorded by the party it is
                    // evidence about. DoNotAllowBypassingSettings is resolved inside the
                    // decision and closes this route to everyone, Admin included.
                    IsBypassRequested = association.IsApprovedByBypass,
                    BypassReason = association.ApprovedByBypassReason,

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
                    $"Association approval denied for {storageAssociation.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");
            }

            return verdict;
        }

        // The answer for the two paths that take no approval decision at all — the workflow's
        // own write, and re-opening a row to Submitted. Permitted, waiving nothing.
        //
        // A verdict is fabricated here only because this service needs the shape back for its
        // bypass log line; every field says the same thing, which is that nothing was decided
        // and nothing was waived.
        // The workflow's verdict: permitted, and carrying whatever waiver the decision it is
        // syncing already recorded. Distinct from NoDecisionVerdict because that one asserts no
        // bypass occurred, which is a claim this actor is in no position to make.
        private static AccessVerdict CarriedBypassVerdict(bool isBypassUsed) =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = isBypassUsed,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = string.Empty,
            };

        private static AccessVerdict NoDecisionVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = string.Empty,
            };

        // Approving OVER the unmet conditions — HR-4 route 3. Structurally the same gate as
        // the one above, and deliberately so: a bypass widens WHICH conditions may be waived,
        // never who is standing at the door. HR-3 and HR-2 still hold, so the row-local
        // publisher-tier check runs first for the same two reasons it does there — an
        // unauthorised caller costs one role comparison instead of four table reads, and a
        // defect in the gathering can only ever make this gate stricter, never open it.
        //
        // Everything about the query matches the approve path except the three members that
        // ARE the bypass: the decision is fixed to Approve (a bypass exists to let something
        // through, and there is nothing to waive in refusing), the request is declared, and
        // the reason travels with it — the client refuses a bypass that carries none.
        //
        // Returns the verdict for the same reason the approve one does, and here it matters
        // more: IsApprovedByBypass and ApprovedByBypassReason are written from it, and the
        // decision may permit WITHOUT waiving anything when the conditions turn out to be met.
        // The Publisher tier: a global Publisher or Admin, or a scoped publisher matching AT
        // LEAST ONE endpoint — the same one-endpoint-is-enough reasoning as the review roles
        // (§14.7 posture A′ rule 2), because the pairing is the thing being decided and a
        // publisher trusted with one end can see both. Reviewer-tier roles are excluded at
        // every tier.
        private static bool HasPublisherRoleForAssociation(
            SecurityContext securityContext,
            Association association) =>
            securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || HasEndpointPublisherRole(
                    securityContext,
                    association.EntityAType,
                    association.EntityAContentType)
                || HasEndpointPublisherRole(
                    securityContext,
                    association.EntityBType,
                    association.EntityBContentType);

        private static bool HasEndpointPublisherRole(
            SecurityContext securityContext,
            EntityType entityType,
            ContentType? contentType)
        {
            if (securityContext.Roles.Contains(Roles.PublisherFor(entityType)))
            {
                return true;
            }

            if (contentType.HasValue is false)
            {
                return false;
            }

            return securityContext.Roles.Contains(
                Roles.PublisherFor(entityType, contentType.Value));
        }

        // Sorting arranges someone's own list — an author ordering the posts inside their own
        // series should not need to fetch a reviewer. An Admin may also reorder, because a
        // takedown-adjacent tidy-up is an administrative act.
        private async ValueTask ValidateUserCanSortStorageAssociationAsync(
            Association storageAssociation,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageAssociation.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to sort " +
                        "this content item association.");
            }
        }

        // The owner is excluded ON PURPOSE, and the exclusion is the point of the operation:
        // a contributor who could set the confidence in their own association to 10 defeats
        // scoring entirely. This is the one gate in the service where being the owner makes a
        // caller LESS able, not more.
        private async ValueTask ValidateUserCanSetStorageAssociationConfidenceAsync(
            Association storageAssociation,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageAssociation.CreatedBy == actorUserId;

            if (isOwner)
            {
                throw new UnauthorizedAssociationException(
                    message: "The owner of a content item association may not set " +
                        "its confidence.");
            }

            bool isPublisherOrAdmin =
                securityContext.Roles.Contains(Roles.Publisher)
                    || securityContext.Roles.Contains(Roles.Admin);

            if (isPublisherOrAdmin is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to set the confidence of " +
                        "this content item association.");
            }
        }

        // Publisher or Admin only, and that restriction is LOAD-BEARING rather than a policy
        // preference: a scope change does not re-open approval, and the stated reason it need
        // not is that only the people who would be re-approving it can make one. Widen this
        // gate and the no-reapproval rule loses its justification.
        private static void ValidateUserCanSetStorageAssociationScope(
            SecurityContext securityContext)
        {
            bool isPublisherOrAdmin =
                securityContext.Roles.Contains(Roles.Publisher)
                    || securityContext.Roles.Contains(Roles.Admin);

            if (isPublisherOrAdmin is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to set the scope of " +
                        "this content item association.");
            }
        }

        // Only a row actually in review can be decided. Approving a Draft would skip the
        // submission the workflow is built around.
        // What a row may be transitioned FROM. Draft is refused because a row reaches it once,
        // at creation — deciding one would skip the submission the workflow is built around.
        // Dismissed is refused because a withdrawn row is not in a round at all.
        //
        // Approved and Rejected ARE admitted here: they are terminal, but terminal means the
        // content is immutable and the way out is narrow, not that the row is unreachable. The
        // override gate is what decides who may act on one, and it has already run.
        private static void ValidateStorageAssociationIsTransitionable(
            Association storageAssociation)
        {
            bool isTransitionable =
                storageAssociation.ApprovalStatus == ApprovalStatus.Submitted
                    || storageAssociation.ApprovalStatus == ApprovalStatus.Approved
                    || storageAssociation.ApprovalStatus == ApprovalStatus.Rejected;

            if (isTransitionable is false)
            {
                throw new InvalidAssociationException(
                    message: "Content item association cannot be approved from status " +
                        $"{storageAssociation.ApprovalStatus}.");
            }
        }

        private static void ValidateStorageAnchorAssociation(
            Association maybeAnchorAssociation,
            Guid anchorAssociationId)
        {
            if (maybeAnchorAssociation is null)
            {
                throw new NotFoundAssociationException(
                    message: "Content item association anchor not found with id: " +
                        $"{anchorAssociationId}.");
            }

            // an unpositioned anchor gives nothing to position relative to
            if (maybeAnchorAssociation.SortOrder.HasValue is false)
            {
                throw new InvalidAssociationException(
                    message: "Content item association anchor has no sort order.");
            }
        }

        // A scope is only meaningful against the endpoint's publication model: a non-versioned
        // entity has exactly one row, so AllVersions and ThisVersionOnly mean the same thing
        // and offering the toggle would imply a choice that does not exist.
        private static void ValidateScopeIsApplicableToEndpoints(
            Association storageAssociation,
            Scope entityAScope,
            Scope entityBScope) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsScopeNotApplicable(storageAssociation.EntityAType, entityAScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsScopeNotApplicable(storageAssociation.EntityBType, entityBScope),
                    Parameter: nameof(Association.EntityBScope)));

        // The same duplicate check an add does. UX_Associations_Pair keys on the EFFECTIVE id,
        // which a scope toggle recomputes, so the row can move onto a key another row already
        // holds. Letting the database raise it would surface as a dependency-validation
        // exception with no indication of which endpoint moved.
        private async ValueTask ValidateAssociationPairIsUnoccupiedAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            IQueryable<Association> allAssociations =
                await this.storageBroker.SelectAllAssociationsAsync(cancellationToken);

            Guid entityAEffectiveId = ResolveEffectiveId(
                association.EntityAScope,
                association.EntityAGroupId,
                association.EntityAKeyId);

            Guid entityBEffectiveId = ResolveEffectiveId(
                association.EntityBScope,
                association.EntityBGroupId,
                association.EntityBKeyId);

            bool isOccupied = allAssociations.Any(other =>
                other.Id != association.Id
                    && other.IsDeleted == false
                    && other.EntityAType == association.EntityAType
                    && other.EntityBType == association.EntityBType
                    && other.UserId == association.UserId
                    && other.EntityAEffectiveId == entityAEffectiveId
                    && other.EntityBEffectiveId == entityBEffectiveId);

            if (isOccupied)
            {
                // deliberately a validation failure, not AlreadyExists. That type wraps a
                // conflict the DATABASE raised and requires the inner exception to prove it;
                // this is the service seeing the collision first, and it is something the
                // caller can fix by choosing a different scope.
                throw new InvalidAssociationException(
                    message: "Content item association already exists for the endpoints " +
                        "this scope change would produce.");
            }
        }

        // mirrors the PERSISTED computed column so the check runs against the same key the
        // index uses
        private static Guid ResolveEffectiveId(Scope scope, Guid groupId, Guid keyId) =>
            scope == Scope.AllVersions ? groupId : keyId;

        // Reported as not-found rather than as a distinct "deleted" error, matching the read
        // posture: a removed id must not be distinguishable from one that never existed, or the
        // transitions become a probe for which associations used to exist.
        private static void ValidateStorageAssociationIsNotDeleted(
            Association storageAssociation,
            Guid associationId)
        {
            if (storageAssociation.IsDeleted)
            {
                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }
        }

        private static dynamic IsNoScopeSupplied(Scope? entityAScope, Scope? entityBScope) => new
        {
            Condition = entityAScope.HasValue is false && entityBScope.HasValue is false,
            Message = "At least one endpoint scope is required."
        };

        private static dynamic IsInvalid(Scope? scope) => new
        {
            Condition = scope.HasValue && Enum.IsDefined(scope.Value) is false,
            Message = "Value is not recognized"
        };

        private static dynamic IsNotAnApprovalTransitionTarget(
            ApprovalStatus approvalStatus) => new
            {
                Condition =
                    approvalStatus != ApprovalStatus.Approved
                        && approvalStatus != ApprovalStatus.Rejected
                        && approvalStatus != ApprovalStatus.Submitted,

                Message = "Approval status must be Submitted, Approved or Rejected."
            };

        // The bypass's narrower form. Bypass exists only to APPROVE over unmet conditions.
        private static dynamic IsBypassWithoutApproval(
            bool isApprovedByBypass,
            ApprovalStatus approvalStatus) => new
            {
                Condition =
                    isApprovedByBypass
                        && approvalStatus != ApprovalStatus.Approved,

                Message = "Bypass requires an approved content item association."
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
                Message = "Is published requires an approved content item association."
            };

        private static dynamic IsPublishDateWithoutPublication(
            bool isPublished,
            DateTimeOffset? publishDate) => new
            {
                Condition = isPublished is false && publishDate.HasValue,
                Message = "Publish date requires a published content item association."
            };

        private static dynamic IsInvalidAnchorId(Guid anchorAssociationId) => new
        {
            Condition = anchorAssociationId == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsSameAssociation(Guid associationId, Guid anchorAssociationId) => new
        {
            Condition = associationId == anchorAssociationId,
            Message = "Anchor must be a different content item association."
        };

        private static dynamic IsInvalid(SortPosition position) => new
        {
            Condition = Enum.IsDefined(position) is false,
            Message = "Value is not recognized"
        };

        private static dynamic IsOutOfRange(decimal? confidenceScore) => new
        {
            Condition = confidenceScore.HasValue
                && (confidenceScore.Value < 0m || confidenceScore.Value > 10m),

            Message = "Confidence score must be between 0 and 10."
        };

        private static dynamic IsProvenanceIncomplete(
            Guid? sourceBatchId,
            string? modelVersion) => new
            {
                Condition = sourceBatchId.HasValue
                    != (string.IsNullOrWhiteSpace(modelVersion) is false),

                Message = "Source batch id and model version must be set together."
            };

        private static dynamic IsProvenanceWithoutScore(
            decimal? confidenceScore,
            Guid? sourceBatchId,
            string? modelVersion) => new
            {
                Condition = confidenceScore.HasValue is false
                    && (sourceBatchId.HasValue
                        || string.IsNullOrWhiteSpace(modelVersion) is false),

                Message = "Provenance requires a confidence score."
            };
    }
}
