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
        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller
        //
        // Association has no scoped roles of its own (design §14.7, §18.6) — authorization
        // derives from its two endpoints. Both tiers compose from the row alone, which is
        // what the denormalised endpoint content type is for: no endpoint is resolved, and
        // no read is issued, to answer an authorization question.

        // The contribution gate. Takes the association because the answer depends on the
        // endpoints, and blocks on EITHER of them.
        //
        // The OR is load-bearing. Under AND, a user holding Tag-ReadOnly alongside
        // BibleReference-Reviewer could pair a tag with an entity type they are not banned
        // from and land it on a public scripture page — precisely what Tag-ReadOnly exists
        // to prevent. A block on one end blocks the association.
        private static void ValidateUserIsAllowedToContribute(
            SecurityContext securityContext,
            Association association)
        {
            ValidateUserIsNotGloballyBlockedFromContributing(securityContext);
            ValidateAssociationIsNotNull(association);

            ValidateUserIsNotBlockedFromEndpoints(
                securityContext: securityContext,
                firstEntityType: association.EntityAType,
                secondEntityType: association.EntityBType);
        }

        // The half of the gate that needs no endpoints: authentication and the global block
        // role. Split out so the remove path — which is handed an id, not an association —
        // can still reject an anonymous or globally-blocked caller before it reads storage.
        // Folding the whole gate below the load would let an anonymous caller probe which
        // ids exist, and would cost a query per rejected request.
        private static void ValidateUserIsNotGloballyBlockedFromContributing(
            SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");
            }
        }

        // The endpoint-scoped half, which needs both entity types. Runs against a
        // caller-supplied association on add and modify, and against the storage row on
        // remove — the same rule either way.
        private static void ValidateUserIsNotBlockedFromEndpoints(
            SecurityContext securityContext,
            EntityType firstEntityType,
            EntityType secondEntityType)
        {
            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnlyFor(firstEntityType))
                    || securityContext.Roles.Contains(Roles.ReadOnlyFor(secondEntityType));

            if (isBlocked)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");
            }
        }

        // the global moderation roles, which grant review over every entity type
        private static bool HasGlobalReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin);

        // Both tiers for one endpoint: the coarse ContentItem-Reviewer from the entity type,
        // and the narrow ContentItem-Testimony-Reviewer from the denormalised content type.
        // The narrow tier only exists for ContentItem, so a null content type simply costs
        // the caller that tier rather than widening anything (design §18.6 rule 4).
        private static bool HasEndpointReviewRole(
            SecurityContext securityContext,
            EntityType entityType,
            ContentType? contentType)
        {
            if (securityContext.Roles.Contains(Roles.ReviewerFor(entityType))
                || securityContext.Roles.Contains(Roles.PublisherFor(entityType)))
            {
                return true;
            }

            if (contentType.HasValue is false)
            {
                return false;
            }

            return securityContext.Roles.Contains(
                    Roles.ReviewerFor(entityType, contentType.Value))
                || securityContext.Roles.Contains(
                    Roles.PublisherFor(entityType, contentType.Value));
        }

        // the moderation roles that may act on and read non-public rows for review and
        // audit: a global elevated role, or a scoped role matching AT LEAST ONE endpoint.
        //
        // One endpoint is enough because a reviewer trusted with tags is trusted to judge
        // whether a tag belongs on something — the pairing is the thing under review, and
        // they can see both ends of it. Requiring both would leave every cross-type
        // association unreviewable by anyone short of a global role.
        private static bool HasReviewRoleForAssociation(
            SecurityContext securityContext,
            Association association) =>
            HasGlobalReviewRole(securityContext)
                || HasEndpointReviewRole(
                    securityContext,
                    association.EntityAType,
                    association.EntityAContentType)
                || HasEndpointReviewRole(
                    securityContext,
                    association.EntityBType,
                    association.EntityBContentType);

        // row-level write permission: the owner or a review role may write the row — the
        // narrower workflow rules stay in the orchestration, which needs owner writes for
        // resubmission and role writes for the publish flip
        // Returns whether the caller may also use the Draft <-> Submitted carve-out (design
        // §9.2 rules 4-6). The answer falls out of the ownership check this method already
        // performs, so it is returned rather than recomputed - a second GetUserIdAsync would
        // be a wasted call and a second chance for the two answers to disagree.
        //
        // Note what the carve-out is NOT gated on: write permission. A Reviewer passes the
        // check below and may amend content, and must still never move an approval status
        // (§8.6 HR-3).
        private async ValueTask<bool> ValidateUserCanModifyStorageAssociationAsync(
            Association storageAssociation,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageAssociation.CreatedBy == actorUserId;

            if (isOwner is false
                && HasReviewRoleForAssociation(securityContext, storageAssociation) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to modify this content item association.");
            }

            return isOwner
                || HasPublisherRoleForAssociation(securityContext, storageAssociation);
        }

        // removing an association is a takedown, not a moderation step — the owner may
        // remove their own association and an Admin may remove anyone's; Reviewers and
        // Publishers moderate through the approval workflow instead
        private async ValueTask ValidateUserCanRemoveStorageAssociationAsync(
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
                    message: "The current user is not allowed to remove this content item association.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only, and additionally
        // subject to the endpoint veto once the row is known (applied by the caller)
        private static void ValidateUserCanHardRemoveAssociation(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to permanently remove " +
                        "this content item association.");
            }
        }

        private async ValueTask ValidateOnAddAssociationAsync(
            Association association,
            SecurityContext securityContext)
        {
            ValidateAssociationIsNotNull(association);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(association.Id),
                    Parameter: nameof(Association.Id)),

                (Rule: IsInvalid(association.EntityAType),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsInvalid(association.EntityAKeyId),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsInvalid(association.EntityAGroupId),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsInvalid(association.EntityBType),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsInvalid(association.EntityBKeyId),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsInvalid(association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsSameEndpoint(
                        association.EntityAGroupId,
                        association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityAType,
                        association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsInvalid(association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityBType,
                        association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsInvalid(association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsInvalid(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)),

                (Rule: IsInvalid(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsGreaterThan(association.CreatedBy, 255),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsGreaterThan(association.UpdatedBy, 255),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsGreaterThan(association.UserId, 255),
                    Parameter: nameof(Association.UserId)),

                (Rule: IsGreaterThan(association.ConfidenceReason, 500),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsGreaterThan(association.ModelVersion, 128),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsNotWithinRange(association.ConfidenceScore, 0, 10),
                    Parameter: nameof(Association.ConfidenceScore)),

                // A row is contributed unpublished, and publication is the approve
                // operation's to grant (design §9.7.1 rules 1 and 3). Without these three
                // rules any authenticated caller can insert a row that is already Approved
                // and IsPublished, which is public the moment it lands — the approval
                // workflow is simply skipped rather than bypassed.
                (Rule: IsSetOnAdd(association.IsPublished),
                    Parameter: nameof(Association.IsPublished)),

                (Rule: IsSetOnAdd(association.PublishDate),
                    Parameter: nameof(Association.PublishDate)),

                (Rule: IsNotContributableStatus(association.ApprovalStatus),
                    Parameter: nameof(Association.ApprovalStatus)),

                (Rule: IsNotSame(
                        firstDate: association.UpdatedWhen,
                        secondDate: association.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsNotSame(
                        first: association.UpdatedBy,
                        second: association.CreatedBy,
                        secondName: nameof(Association.CreatedBy)),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: await IsNotRecentAsync(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyAssociationAsync(
            Association association,
            SecurityContext securityContext)
        {
            ValidateAssociationIsNotNull(association);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsInvalid(association.Id),
                    Parameter: nameof(Association.Id)),

                (Rule: IsInvalid(association.EntityAType),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsInvalid(association.EntityAKeyId),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsInvalid(association.EntityAGroupId),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsInvalid(association.EntityBType),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsInvalid(association.EntityBKeyId),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsInvalid(association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsSameEndpoint(
                        association.EntityAGroupId,
                        association.EntityBGroupId),
                    Parameter: nameof(Association.EntityBGroupId)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityAType,
                        association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsInvalid(association.EntityAContentType),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsContentTypeNotApplicable(
                        association.EntityBType,
                        association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.EntityBContentType),
                    Parameter: nameof(Association.EntityBContentType)),

                (Rule: IsInvalid(association.CreatedBy),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsInvalid(association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsInvalid(association.CreatedWhen),
                    Parameter: nameof(Association.CreatedWhen)),

                (Rule: IsInvalid(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: IsGreaterThan(association.CreatedBy, 255),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsGreaterThan(association.UpdatedBy, 255),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsGreaterThan(association.UserId, 255),
                    Parameter: nameof(Association.UserId)),

                (Rule: IsGreaterThan(association.ConfidenceReason, 500),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsGreaterThan(association.ModelVersion, 128),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsNotWithinRange(association.ConfidenceScore, 0, 10),
                    Parameter: nameof(Association.ConfidenceScore)),

                // Scope is pinned against storage further down — set-scope owns it (design
                // §9.7.1 rule 6). It is still checked structurally here rather than
                // re-derived, for two reasons: re-deriving would silently overwrite a
                // narrowing that set-scope legitimately made, and these structural rules run
                // BEFORE the storage pin, so a malformed value is reported as the unsupported
                // scope it is instead of as a mismatch with the stored row. The endpoint type
                // is pinned too, so checking the scope against the input's own type is sound.
                (Rule: IsInvalid(association.EntityAScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsScopeNotApplicable(
                        association.EntityAType,
                        association.EntityAScope),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsInvalid(association.EntityBScope),
                    Parameter: nameof(Association.EntityBScope)),

                (Rule: IsScopeNotApplicable(
                        association.EntityBType,
                        association.EntityBScope),
                    Parameter: nameof(Association.EntityBScope)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: association.UpdatedBy),
                    Parameter: nameof(Association.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: association.UpdatedWhen,
                        secondDate: association.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(association.UpdatedWhen),
                    Parameter: nameof(Association.UpdatedWhen)));
        }

        private static void ValidateAssociationEventEnvelope(
            EventEnvelope<Association> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");
            }
        }

        // reclassification is forbidden: an association is a link between two specific
        // entities, and repointing it is indistinguishable from deleting one link and
        // creating another — except that it carries the original's approval state and
        // review history across to a pair nobody reviewed. Type, KeyId and GroupId are
        // therefore pinned against storage on both endpoints. Scope is pinned here as well:
        // it may still change, but only through the set-scope operation (design §9.7.1
        // rule 6), whose Publisher/Admin gate this modify would otherwise route around.
        private static void ValidateAgainstStorageAssociationOnModify(
            Association inputAssociation,
            Association storageAssociation,
            bool mayTransitionApprovalStatus)
        {
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputAssociation.CreatedWhen,
                        secondDate: storageAssociation.CreatedWhen,
                        secondDateName: nameof(Association.CreatedWhen)),
                    Parameter: nameof(Association.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputAssociation.CreatedBy,
                        second: storageAssociation.CreatedBy,
                        secondName: nameof(Association.CreatedBy)),
                    Parameter: nameof(Association.CreatedBy)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAType,
                        second: storageAssociation.EntityAType,
                        secondName: nameof(Association.EntityAType)),
                    Parameter: nameof(Association.EntityAType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAKeyId,
                        second: storageAssociation.EntityAKeyId,
                        secondName: nameof(Association.EntityAKeyId)),
                    Parameter: nameof(Association.EntityAKeyId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAGroupId,
                        second: storageAssociation.EntityAGroupId,
                        secondName: nameof(Association.EntityAGroupId)),
                    Parameter: nameof(Association.EntityAGroupId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBType,
                        second: storageAssociation.EntityBType,
                        secondName: nameof(Association.EntityBType)),
                    Parameter: nameof(Association.EntityBType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBKeyId,
                        second: storageAssociation.EntityBKeyId,
                        secondName: nameof(Association.EntityBKeyId)),
                    Parameter: nameof(Association.EntityBKeyId)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBGroupId,
                        second: storageAssociation.EntityBGroupId,
                        secondName: nameof(Association.EntityBGroupId)),
                    Parameter: nameof(Association.EntityBGroupId)),

                // the content type is pinned alongside the identity fields, not left with
                // scope. It is an authorization input (design §18.6) derived from the
                // resolved endpoint, and the endpoint cannot change — so neither can it.
                // Leaving it writable would let an approved association be re-labelled into
                // a content type whose reviewers never saw it.
                (Rule: IsNotSame(
                        first: inputAssociation.EntityAContentType,
                        second: storageAssociation.EntityAContentType,
                        secondName: nameof(Association.EntityAContentType)),
                    Parameter: nameof(Association.EntityAContentType)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBContentType,
                        second: storageAssociation.EntityBContentType,
                        secondName: nameof(Association.EntityBContentType)),
                    Parameter: nameof(Association.EntityBContentType)),

                // The general modify is for content only. Every IApproval member belongs to
                // the approve operation (design §9.7.1 rules 2 and 3), so all three are
                // pinned here rather than carried.
                //
                // This matters more since authorization became endpoint-derived: the write
                // gate now admits any scoped reviewer for EITHER endpoint, so without these
                // pins a Tag-Reviewer could take someone else's pending association and
                // publish it through the general modify — approving content nobody with
                // authority over it ever looked at.
                (Rule: IsNotAPermittedStatusChangeOnModify(
                        inputStatus: inputAssociation.ApprovalStatus,
                        storageStatus: storageAssociation.ApprovalStatus,
                        mayTransition: mayTransitionApprovalStatus),
                    Parameter: nameof(Association.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: inputAssociation.IsPublished,
                        second: storageAssociation.IsPublished,
                        secondName: nameof(Association.IsPublished)),
                    Parameter: nameof(Association.IsPublished)),

                (Rule: IsNotSame(
                        firstDate: inputAssociation.PublishDate,
                        secondDate: storageAssociation.PublishDate,
                        secondDateName: nameof(Association.PublishDate)),
                    Parameter: nameof(Association.PublishDate)),

                // Sorting, confidence, scope and the reacting user are not content either, and
                // each has its own gated operation. Leaving them writable here routes straight
                // around those gates: an author could score their own association through
                // modify, or widen its reach, without ever meeting the Publisher tier the
                // dedicated operations require. Design §9.7.1 - modify is content only.
                (Rule: IsNotSame(
                        first: inputAssociation.SortOrder,
                        second: storageAssociation.SortOrder,
                        secondName: nameof(Association.SortOrder)),
                    Parameter: nameof(Association.SortOrder)),

                (Rule: IsNotSame(
                        first: inputAssociation.ConfidenceScore,
                        second: storageAssociation.ConfidenceScore,
                        secondName: nameof(Association.ConfidenceScore)),
                    Parameter: nameof(Association.ConfidenceScore)),

                (Rule: IsNotSame(
                        first: inputAssociation.ConfidenceReason,
                        second: storageAssociation.ConfidenceReason,
                        secondName: nameof(Association.ConfidenceReason)),
                    Parameter: nameof(Association.ConfidenceReason)),

                (Rule: IsNotSame(
                        first: inputAssociation.SourceBatchId,
                        second: storageAssociation.SourceBatchId,
                        secondName: nameof(Association.SourceBatchId)),
                    Parameter: nameof(Association.SourceBatchId)),

                (Rule: IsNotSame(
                        first: inputAssociation.ModelVersion,
                        second: storageAssociation.ModelVersion,
                        secondName: nameof(Association.ModelVersion)),
                    Parameter: nameof(Association.ModelVersion)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityAScope,
                        second: storageAssociation.EntityAScope,
                        secondName: nameof(Association.EntityAScope)),
                    Parameter: nameof(Association.EntityAScope)),

                (Rule: IsNotSame(
                        first: inputAssociation.EntityBScope,
                        second: storageAssociation.EntityBScope,
                        secondName: nameof(Association.EntityBScope)),
                    Parameter: nameof(Association.EntityBScope)),

                (Rule: IsNotSame(
                        first: inputAssociation.UserId,
                        second: storageAssociation.UserId,
                        secondName: nameof(Association.UserId)),
                    Parameter: nameof(Association.UserId)),

                (Rule: IsSame(
                        firstDate: inputAssociation.UpdatedWhen,
                        secondDate: storageAssociation.UpdatedWhen,
                        secondDateName: nameof(Association.UpdatedWhen)),
                    Parameter: nameof(Association.UpdatedWhen)));
        }

        private static void ValidateOnRetrieveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateOnRemoveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateOnHardRemoveAssociationById(Guid associationId) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(associationId), Parameter: nameof(Association.Id)));

        private static void ValidateStorageAssociation(
            Association maybeAssociation,
            Guid associationId)
        {
            if (maybeAssociation is null)
            {
                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }
        }

        private static void ValidateAssociationIsNotNull(
            Association association)
        {
            if (association is null)
            {
                throw new NullAssociationException(message: "Content item association is null.");
            }
        }

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        // structural validation for an enum crossing a boundary — rejects an out-of-range
        // value (a stale client sending a since-removed member); it cannot detect "caller
        // forgot to set it", since EntityType has no unset sentinel
        private static dynamic IsInvalid(EntityType entityType) => new
        {
            Condition = Enum.IsDefined(entityType) == false,
            Message = "Value is not a supported entity type"
        };

        private static dynamic IsInvalid(Scope scope) => new
        {
            Condition = Enum.IsDefined(scope) == false,
            Message = "Value is not a supported scope"
        };

        // only a supplied content type is range checked — null is the ordinary state for
        // every endpoint that is not a ContentItem, and for a ContentItem whose type the
        // orchestration has not resolved yet
        private static dynamic IsInvalid(ContentType? contentType) => new
        {
            Condition = contentType.HasValue && Enum.IsDefined(contentType.Value) == false,
            Message = "Value is not a supported content type"
        };

        // a non-versioned entity type has exactly one row, so AllVersions cannot mean
        // anything for it. On add this is unreachable because the scope is derived; on
        // modify the caller supplies it, and the derived invariant has to be defended
        // rather than silently re-imposed (design §9.7.1 rule 6).
        private static dynamic IsScopeNotApplicable(
            EntityType entityType,
            Scope scope) => new
            {
                Condition = Enum.IsDefined(entityType)
                    && EntityTypeVersioning.IsVersioned(entityType) is false
                    && scope == Scope.AllVersions,

                Message = "Value is only applicable to a versioned endpoint"
            };

        // one rule covers three mistakes at once: associating an entity with itself, with
        // another version of itself, and — since a non-versioned endpoint's group id is its
        // key id — a tag with itself
        private static dynamic IsSameEndpoint(
            Guid firstGroupId,
            Guid secondGroupId) => new
            {
                Condition = firstGroupId != Guid.Empty && firstGroupId == secondGroupId,
                Message = $"Value is the same as {nameof(Association.EntityAGroupId)}"
            };

        // only a ContentItem endpoint has a content type (design §18.6): no other entity
        // type has a sub-classification, so a value here on any other type is a caller
        // fabricating an authorization input. A ContentItem endpoint may still be null —
        // resolving it needs the endpoint row, which is the orchestration's read to make,
        // and a null simply costs the caller the narrow role tier.
        private static dynamic IsContentTypeNotApplicable(
            EntityType entityType,
            ContentType? contentType) => new
            {
                Condition = contentType is not null && entityType != EntityType.ContentItem,
                Message = $"Value is only applicable to a {nameof(EntityType.ContentItem)} endpoint"
            };

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            string first,
            string second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Text is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsNotSame(
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            EntityType first,
            EntityType second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            ContentType? first,
            ContentType? second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            ApprovalStatus first,
            ApprovalStatus second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            bool first,
            bool second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset? firstDate,
            DateTimeOffset? secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        // a contribution is unpublished by definition — publication is granted by the
        // approve operation, never asserted by the contributor
        private static dynamic IsNotSame(
            Scope first,
            Scope second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            int? first,
            int? second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            decimal? first,
            decimal? second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            Guid? first,
            Guid? second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        // The one carve-out on modify (design §9.2 rules 4-6): the owner may move the status
        // between Draft and Submitted, because submitting is inseparable from the edit that
        // made the work ready. Everything else about the status stays pinned, and the caller
        // must have been found eligible for the carve-out before this is reached - a Reviewer
        // holds write permission on the row and must still never move the status (HR-3).
        private static dynamic IsNotAPermittedStatusChangeOnModify(
            ApprovalStatus inputStatus,
            ApprovalStatus storageStatus,
            bool mayTransition) => new
            {
                Condition =
                    inputStatus != storageStatus
                        && (mayTransition is false
                            || IsDraftOrSubmitted(inputStatus) is false
                            || IsDraftOrSubmitted(storageStatus) is false),

                Message = "Value is not the same as storage approval status"
            };

        private static bool IsDraftOrSubmitted(ApprovalStatus approvalStatus) =>
            approvalStatus == ApprovalStatus.Draft
                || approvalStatus == ApprovalStatus.Submitted;

        private static dynamic IsSetOnAdd(bool value) => new
        {
            Condition = value,
            Message = "Value is not allowed on add"
        };

        private static dynamic IsSetOnAdd(DateTimeOffset? date) => new
        {
            Condition = date is not null,
            Message = "Date is not allowed on add"
        };

        // a caller may save work in progress or submit it for review; the remaining states
        // are verdicts, and a verdict is the approval workflow's to record (design §9.7.1
        // rule 1)
        private static dynamic IsNotContributableStatus(ApprovalStatus approvalStatus) => new
        {
            Condition = approvalStatus != ApprovalStatus.Draft
                && approvalStatus != ApprovalStatus.Submitted,

            Message = $"Value must be {nameof(ApprovalStatus.Draft)} " +
                $"or {nameof(ApprovalStatus.Submitted)} on add"
        };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        // the score is optional — only a supplied value is range checked. Null means not yet
        // scored, which is not the same as a zero and must not be treated as one.
        private static dynamic IsNotWithinRange(decimal? value, decimal minimum, decimal maximum) => new
        {
            Condition = value.HasValue && (value < minimum || value > maximum),
            Message = $"Value is not within range of {minimum} and {maximum}"
        };

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
            };

        private async ValueTask<dynamic> IsNotRecentAsync(DateTimeOffset date)
        {
            var (isNotRecent, startDate, endDate) = await IsDateNotRecentAsync(date);

            return new
            {
                Condition = isNotRecent,
                Message = $"Date is not recent. Expected a value between {startDate} and {endDate} but found {date}"
            };
        }

        private async ValueTask<(bool IsNotRecent, DateTimeOffset StartDate, DateTimeOffset EndDate)>
            IsDateNotRecentAsync(DateTimeOffset date)
        {
            int pastThreshold = 90;
            int futureThreshold = 0;
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            DateTimeOffset startDate = currentDateTime.AddSeconds(-pastThreshold);
            DateTimeOffset endDate = currentDateTime.AddSeconds(futureThreshold);
            bool isNotRecent = date < startDate || date > endDate;

            return (isNotRecent, startDate, endDate);
        }

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidAssociationException = new InvalidAssociationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidAssociationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidAssociationException.ThrowIfContainsErrors();
        }
    }
}
