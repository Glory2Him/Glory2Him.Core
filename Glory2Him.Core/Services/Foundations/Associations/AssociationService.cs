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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    /// <summary>
    /// Foundation service for content item associations. Every operation is both callable
    /// directly (the non-event path: object in → request envelope → shared do-work) and
    /// reachable through the event substrate (the event path in the <c>.Substrate</c> partial:
    /// request envelope in → shared do-work). The private <c>DoXAsync</c> methods own auditing,
    /// validation, storage, and publishing the past-tense fact, so the two paths cannot
    /// diverge; the inbound envelope carries the original caller's <c>SecurityContext</c> and
    /// anchors the causation chain. Per design §14.6 the foundation enforces security itself,
    /// never assuming an upstream orchestration already gated the caller.
    ///
    /// <para><b>Security posture A′ (design §14.7).</b> An association has no scoped roles of
    /// its own; every scoped question is answered from its two endpoints, using only columns
    /// on the row. The contribution gate blocks on the global <c>ReadOnly</c> <b>or</b> a
    /// <c>ReadOnly</c> scoped to <i>either</i> endpoint type. Review permission is a global
    /// elevated role <b>or</b> a scoped role matching <i>at least one</i> endpoint, each
    /// endpoint checked at both the coarse entity-type tier and the narrow content-type tier.
    /// Write permission is the owner or a review role; removal is the owner or <c>Admin</c>,
    /// hard removal <c>Admin</c> only — both additionally subject to the endpoint veto. The
    /// veto is scoped to writes and never consulted on a read, so a moderator holding one
    /// scoped <c>ReadOnly</c> keeps their audit visibility. Reads otherwise follow the
    /// §14.1/§14.5 posture.</para>
    /// </summary>
    internal partial class AssociationService : IAssociationService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public AssociationService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<Association> AddAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoAddAssociationAsync(
                    association: association,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Association());

                IQueryable<Association> allAssociations =
                    await this.storageBroker.SelectAllAssociationsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    associations: allAssociations,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Association> RetrieveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var retrieveRequest = new Association { Id = associationId };

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveAssociationByIdAsync(
                    associationId: associationId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> ModifyAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoModifyAssociationAsync(
                    association: association,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> RemoveAssociationByIdAsync(
            Guid associationId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var removeRequest = new Association { Id = associationId, DeletionReason = deletionReason };

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveAssociationByIdAsync(
                    associationId: associationId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> HardRemoveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var hardRemoveRequest = new Association { Id = associationId };

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveAssociationByIdAsync(
                    associationId: associationId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<Association> DoRetrieveAssociationByIdAsync(
            Guid associationId,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveAssociationById(associationId);

            Association maybeAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: associationId,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociation(maybeAssociation, associationId);

            if (maybeAssociation.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{associationId} is soft-deleted; reported to the caller as not found.");

                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeAssociation.ApprovalStatus == ApprovalStatus.Approved
                    && maybeAssociation.IsPublished
                    && (maybeAssociation.PublishDate is null
                        || maybeAssociation.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeAssociation;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{associationId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found.");

                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeAssociation.CreatedBy == actorUserId;

            // the review check is endpoint-derived and the row is in hand, so a reviewer
            // scoped to either end can audit it. The contribution veto is deliberately NOT
            // consulted here: design §18.6 defines ReadOnly as a contribution block, and
            // treating it as a read block would strip audit visibility from a moderator who
            // happens to hold one scoped ReadOnly.
            if (isOwner is false
                && HasReviewRoleForAssociation(securityContext, maybeAssociation) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item association read denied. Content item association " +
                        $"{associationId} is not publicly visible and user \"{actorUserId}\" " +
                        "is neither the owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundAssociationException(
                    message: $"Content item association not found with id: {associationId}.");
            }

            return maybeAssociation;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<Association>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Association> associations,
            SecurityContext? securityContext)
        {
            IQueryable<Association> visibleAssociations =
                associations.Where(association =>
                    association.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasGlobalReviewRole(securityContext!))
            {
                return visibleAssociations;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnAssociations = string.IsNullOrWhiteSpace(actorUserId) is false;

            // This filter has no row to inspect — it composes an expression tree — so the
            // scoped roles are resolved in memory FIRST and the resulting sets are closed
            // over. `Contains` on a local collection translates to `IN (...)`, and because
            // both enums persist as strings EF parameterises the converted values.
            //
            // A caller with no scoped roles gets two empty sets, and an empty
            // `HashSet.Contains` is constant-false, so the query degrades to exactly the
            // public-plus-own predicate it had before.
            HashSet<EntityType> reviewableEntityTypes =
                ResolveReviewableEntityTypes(securityContext);

            HashSet<ContentType> reviewableContentTypes =
                ResolveReviewableContentTypes(securityContext);

            return visibleAssociations.Where(association =>
                reviewableEntityTypes.Contains(association.EntityAType)
                || reviewableEntityTypes.Contains(association.EntityBType)
                || (association.EntityAType == EntityType.ContentItem
                    && association.EntityAContentType != null
                    && reviewableContentTypes.Contains(association.EntityAContentType.Value))
                || (association.EntityBType == EntityType.ContentItem
                    && association.EntityBContentType != null
                    && reviewableContentTypes.Contains(association.EntityBContentType.Value))
                || (association.ApprovalStatus == ApprovalStatus.Approved
                    && association.IsPublished
                    && (association.PublishDate == null
                        || association.PublishDate <= currentDateTime))
                || (includeOwnAssociations
                    && association.CreatedBy == actorUserId));
        }

        // the entity types this caller may review, from the coarse tier. Enumerating the
        // enum rather than parsing the caller's role strings means an unrecognised role
        // simply never matches, instead of being split on '-' into something that might.
        private static HashSet<EntityType> ResolveReviewableEntityTypes(
            SecurityContext? securityContext)
        {
            var reviewableEntityTypes = new HashSet<EntityType>();

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                return reviewableEntityTypes;
            }

            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                bool isReviewable =
                    securityContext.Roles.Contains(Roles.ReviewerFor(entityType))
                        || securityContext.Roles.Contains(Roles.PublisherFor(entityType));

                if (isReviewable)
                {
                    reviewableEntityTypes.Add(entityType);
                }
            }

            return reviewableEntityTypes;
        }

        // the narrow tier. Only ContentItem carries a content type (design §18.6 rule 5), so
        // only ContentItem-scoped role names are composed.
        //
        // The caller of this set must still test the endpoint TYPE alongside the content
        // type. It is tempting not to: `IsContentTypeNotApplicable` already refuses a content
        // type on any other endpoint, so the column "cannot" be populated on a Tag. But that
        // invariant lives in this service, not in the schema — there is no check constraint
        // tying the column to an EntityType of ContentItem — so a row arriving by migration,
        // backfill or direct SQL is not bound by it. Matching on the content type alone would
        // then hand a "ContentItem-Testimony-Reviewer" a Tag endpoint carrying Testimony,
        // while the single read denies the same row (it composes "Tag-Testimony-Reviewer",
        // which is never granted). The bulk path must not be the more permissive of the two.
        private static HashSet<ContentType> ResolveReviewableContentTypes(
            SecurityContext? securityContext)
        {
            var reviewableContentTypes = new HashSet<ContentType>();

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                return reviewableContentTypes;
            }

            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                bool isReviewable =
                    securityContext.Roles.Contains(
                            Roles.ReviewerFor(EntityType.ContentItem, contentType))
                        || securityContext.Roles.Contains(
                            Roles.PublisherFor(EntityType.ContentItem, contentType));

                if (isReviewable)
                {
                    reviewableContentTypes.Add(contentType);
                }
            }

            return reviewableContentTypes;
        }

        private async ValueTask<Association> DoAddAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext, association);

            association = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(
                    entity: association,
                    securityContext: inboundEnvelope.SecurityContext);

            // scope and the non-versioned group id are the service's to derive, never the
            // caller's to supply — so they are settled before validation reports on them
            association = ApplyDerivedEndpointFields(association);

            await ValidateOnAddAssociationAsync(
                association: association,
                securityContext: inboundEnvelope.SecurityContext);

            // canonical ordering lands here rather than in the public method or an
            // orchestration: `Association-Adding` is a public event address whose substrate
            // handler enters DoAdd directly, so anything layered above it is bypassed.
            // Validation runs first so its messages name the endpoints the caller sent.
            association = NormalizeEndpointOrder(association);

            Association addedAssociation =
                await this.storageBroker.InsertAssociationAsync(
                    association,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnAddingAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedAssociation);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: AssociationEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnAddingAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            return addedAssociation;
        }

        private async ValueTask<Association> DoModifyAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext, association);

            association = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: association,
                    securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyAssociationAsync(
                association: association,
                securityContext: inboundEnvelope.SecurityContext);

            Association maybeAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: association.Id,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociation(
                maybeAssociation,
                associationId: association.Id);

            bool mayTransitionApprovalStatus =
                await ValidateUserCanModifyStorageAssociationAsync(
                    storageAssociation: maybeAssociation,
                    securityContext: inboundEnvelope.SecurityContext);

            association = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: association,
                    storageEntity: maybeAssociation);

            ValidateAgainstStorageAssociationOnModify(
                inputAssociation: association,
                storageAssociation: maybeAssociation,
                mayTransitionApprovalStatus: mayTransitionApprovalStatus);

            Association updatedAssociation =
                await this.storageBroker.UpdateAssociationAsync(
                    association,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnModifyingAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedAssociation);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: AssociationEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnModifyingAssociationSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedAssociation;
        }

        private async ValueTask<Association> DoRemoveAssociationByIdAsync(
            Guid associationId,
            string? deletionReason,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // only the endpoint-independent half of the contribution gate can run here — the
            // scoped veto needs the row, and this path is handed an id. Keeping the
            // authentication and global-block checks above the read means an anonymous or
            // globally blocked caller never reaches the Associations table, so this surface
            // cannot be used to probe which association ids exist. (The event path first
            // touches ProcessedEvents for deduplication; that lookup is keyed on the event
            // id, not the association id, so it reveals nothing about which rows exist.)
            ValidateUserIsNotGloballyBlockedFromContributing(inboundEnvelope.SecurityContext);
            ValidateOnRemoveAssociationById(associationId);

            Association maybeAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: associationId,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociation(maybeAssociation, associationId);

            // the endpoint veto, now that both endpoints are known
            ValidateUserIsNotBlockedFromEndpoints(
                securityContext: inboundEnvelope.SecurityContext,
                firstEntityType: maybeAssociation.EntityAType,
                secondEntityType: maybeAssociation.EntityBType);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageAssociationAsync(
                storageAssociation: maybeAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeAssociation.IsDeleted)
                return maybeAssociation;

            if (deletionReason is not null)
                maybeAssociation.DeletionReason = deletionReason;

            Association auditedAssociation =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeAssociation,
                    securityContext: inboundEnvelope.SecurityContext);

            Association removedAssociation =
                await this.storageBroker.UpdateAssociationAsync(
                    association: auditedAssociation,
                    cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnRemovingAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedAssociation);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: AssociationEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnRemovingAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedAssociation;
        }

        private async ValueTask<Association> DoHardRemoveAssociationByIdAsync(
            Guid associationId,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveAssociation(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveAssociationById(associationId);

            Association maybeAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: associationId,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociation(maybeAssociation, associationId);

            // the same endpoint veto the soft delete applies. Without it this surface is the
            // one write an endpoint-blocked Admin can still perform — and it is the
            // destructive one, taking the audit trail with it. A block that stops the
            // reversible takedown but not the irreversible one is the wrong way round.
            ValidateUserIsNotBlockedFromEndpoints(
                securityContext: inboundEnvelope.SecurityContext,
                firstEntityType: maybeAssociation.EntityAType,
                secondEntityType: maybeAssociation.EntityBType);

            Association deletedAssociation =
                await this.storageBroker.DeleteAssociationAsync(
                    maybeAssociation,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnHardRemovingAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedAssociation);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: AssociationEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnHardRemovingAssociationByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedAssociation;
        }
    }
}
