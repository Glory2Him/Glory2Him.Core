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
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    /// <summary>
    /// The narrow state-transition operations (design §9.7.1, §9.2).
    ///
    /// <para>The general modify is content-only. Each field group that is not content gets its
    /// own operation here, owning exactly its own fields and publishing its own fact. That
    /// separation is the approval workflow's cycle-breaker: the workflow subscribes to
    /// <c>Modified</c> and causes <c>Approved</c>, so a transition that published
    /// <c>Modified</c> would re-enter the handler that caused it. <c>ProcessedEvents</c> cannot
    /// help — it is keyed on the event id, and a write-back mints a fresh one — and under
    /// inline dispatch the repetition is synchronous re-entry inside the originating
    /// request.</para>
    ///
    /// <para>Every operation here follows the same order, which differs from
    /// <c>DoModifyAssociationAsync</c> in one important way: the row is loaded FIRST and the
    /// caller's entity is never the thing saved. Authorization is decided against the STORED
    /// endpoints, because the endpoint content type is an authorization input and a
    /// caller-supplied one would be self-certification. Only the operation's own fields are
    /// then copied onto the stored row.</para>
    /// </summary>
    internal partial class AssociationService
    {
        // Sparse spacing (design §9.7.1 rule 4). Positions are 100, 200, 300 …, so landing
        // beside an anchor is a half-step away — which, at the default spacing, is exactly the
        // midpoint between the anchor and its neighbour, without this method needing to read
        // the neighbour. Repeated subdivision narrows the gap and can eventually produce a
        // tie; ties are legal (SortOrder is deliberately not unique) and fall through the
        // §11.7 tie-break chain. A dense sequence would instead force renumbering every row
        // after the insertion point, which is multi-row work and belongs at orchestration.
        private const int SortOrderStep = 100;

        public ValueTask<Association> TransitionAssociationApprovalAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoTransitionAssociationApprovalAsync(
                    association: association,
                    inboundEnvelope: envelope,

                    // This envelope's context was minted here, in process, from the ambient
                    // caller — so a system identity on it is one this process asserted about
                    // itself, so a system identity on it is one this process asserted about
                    // itself. The event path admits the claim too, because only this
                    // system holds the signing key — so a verified envelope is one this
                    // system minted, whichever path it arrived by (§16.7.1).
                    isSystemIdentityAdmissible: true,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> SortAssociationAsync(
            Association association,
            Association anchorAssociation,
            SortPosition position,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);
                ValidateAnchorAssociationIsNotNull(anchorAssociation);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoSortAssociationAsync(
                    association: association,
                    anchorAssociation: anchorAssociation,
                    position: position,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> SetAssociationConfidenceAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoSetAssociationConfidenceAsync(
                    association: association,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Association> SetAssociationScopeAsync(
            Guid associationId,
            Scope? entityAScope,
            Scope? entityBScope,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var scopeRequest = new Association { Id = associationId };

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: scopeRequest);

                return await DoSetAssociationScopeAsync(
                    associationId: associationId,
                    entityAScope: entityAScope,
                    entityBScope: entityBScope,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Association> DoTransitionAssociationApprovalAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            bool isSystemIdentityAdmissible,
            CancellationToken cancellationToken)
        {
            ValidateUserIsNotGloballyBlockedFromContributing(inboundEnvelope.SecurityContext);

            // Shape first, and the bypass reason with it, so an unexplained bypass is refused
            // before any policy is read — under every policy, including one that would have
            // permitted the waiver.
            ValidateOnTransitionAssociationApproval(association);

            // The system identity is a claim about PROVENANCE, and provenance is not carried by
            // the payload. It is honoured only where this service minted the context itself; an
            // envelope that arrived over a public event address carries a deserialized,
            // unverified context (§14.6 rule 4), and a caller able to assert the flag there
            // would walk past every rule below by declaring themselves the workflow.
            bool isSystemIdentity =
                isSystemIdentityAdmissible
                    && inboundEnvelope.SecurityContext.IsSystemIdentity;

            Association storageAssociation =
                await LoadTransitionTargetAsync(
                    associationId: association.Id,
                    securityContext: inboundEnvelope.SecurityContext,
                    cancellationToken: cancellationToken);

            // decided against the STORED endpoints. Transitioning from the caller's copy would
            // let a contributor claim an endpoint content type they hold a reviewer role for and
            // approve their own row — and would let anyone present a terminal row as Submitted
            // to slip past the override gate.
            AccessVerdict accessVerdict =
                await ValidateUserCanTransitionStorageAssociationApprovalAsync(
                    storageAssociation: storageAssociation,
                    association: association,
                    securityContext: inboundEnvelope.SecurityContext,
                    isSystemIdentity: isSystemIdentity,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociationIsTransitionable(storageAssociation);

            // What the row keeps is THAT the conditions were waived; what it cannot keep is
            // WHICH one, and that is the question an auditor actually asks — waiving a standing
            // rejection and waiving a short approval count are the difference between an
            // incident and a routine launch-day override. The verdict already knows, because
            // the conditions are evaluated on the bypass path purely so it can report this.
            //
            // Server-side only (§14.5): the explanation names resolved policy values, so it
            // must not travel outward on an exception or its Data. Guarded on IsBypassUsed for
            // the same reason the derived pair below is — a bypass can be asked for and turn
            // out to be unnecessary, and logging one then would record an event that never
            // happened.
            if (accessVerdict.IsBypassUsed)
            {
                await this.loggingBroker.LogInformationAsync(
                    $"Association bypass approval granted for {storageAssociation.Id}. "
                        + $"Waived {accessVerdict.BypassedBlockReason}. "
                        + $"Reason given: \"{association.ApprovedByBypassReason}\". "
                        + $"{accessVerdict.Explanation}");
            }

            // the whole of IApproval, as one unit — approve and publish are one operation, so
            // there is no separate publish verb and PublishDate belongs here and nowhere else
            storageAssociation.ApprovalStatus = association.ApprovalStatus;

            // Publication is DERIVED, not copied. Any target but Approved unpublishes the row,
            // so an override out of Approved cannot leave a re-opened row publicly visible while
            // it waits for a second verdict. The validation above already refuses the inverse
            // pairing, which makes this a backstop rather than the only guard — but it is what
            // makes the rule true by construction instead of true by validator.
            //
            // Nothing republishes whatever this may have demoted: the group simply has no public
            // row until something is approved again (epic decision 7).
            bool isApproved = association.ApprovalStatus == ApprovalStatus.Approved;
            storageAssociation.IsPublished = isApproved && association.IsPublished;

            storageAssociation.PublishDate = storageAssociation.IsPublished
                ? association.PublishDate
                : null;

            // The bypass pair, DERIVED from the verdict and never read off the caller's entity.
            // That is the whole point of the pair: they record that the conditions were waived,
            // and a caller able to write them is equally able to clear them.
            //
            // The verdict can legitimately come back IsBypassUsed = false even though a bypass
            // was requested — if the conditions happened to be met, the decision permits without
            // waiving anything — and in that case the row must record no bypass at all.
            // Hardcoding true would manufacture an audit entry for a waiver that never happened,
            // which is as misleading as losing one.
            storageAssociation.IsApprovedByBypass = accessVerdict.IsBypassUsed;

            storageAssociation.ApprovedByBypassReason = accessVerdict.IsBypassUsed
                ? association.ApprovedByBypassReason
                : null;

            // The fact follows the DECISION, not the operation's name. A rejection broadcast
            // on the Approved address would tell every subscriber the opposite of what
            // happened, and the fact name is the contract they key on. An override back to
            // Submitted re-opens the round, which is what the Submitted address already means.
            //
            // A bypass approval has no fact of its own and never did: it IS an approval to every
            // subscriber, and the waiver travels on the row. A second address would split the
            // audience for one outcome and leave a consumer subscribed to Approved alone
            // silently missing exactly the approvals most worth seeing.
            AssociationEventOperation decision = association.ApprovalStatus switch
            {
                ApprovalStatus.Approved => AssociationEventOperation.Approved,
                ApprovalStatus.Rejected => AssociationEventOperation.Rejected,
                _ => AssociationEventOperation.Submitted
            };

            return await SaveTransitionAsync(
                association: storageAssociation,
                inboundEnvelope: inboundEnvelope,
                operation: decision,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnApprovingAssociationSubscriptionName,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Association> DoSortAssociationAsync(
            Association association,
            Association anchorAssociation,
            SortPosition position,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsNotGloballyBlockedFromContributing(inboundEnvelope.SecurityContext);
            ValidateOnSortAssociation(association, anchorAssociation, position);

            Association storageAssociation =
                await LoadTransitionTargetAsync(
                    associationId: association.Id,
                    securityContext: inboundEnvelope.SecurityContext,
                    cancellationToken: cancellationToken);

            Association storageAnchorAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: anchorAssociation.Id,
                    cancellationToken: cancellationToken);

            ValidateStorageAnchorAssociation(
                storageAnchorAssociation,
                anchorAssociationId: anchorAssociation.Id);

            await ValidateUserCanSortStorageAssociationAsync(
                storageAssociation: storageAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            storageAssociation.SortOrder =
                ResolveSortOrder(storageAnchorAssociation, position);

            return await SaveTransitionAsync(
                association: storageAssociation,
                inboundEnvelope: inboundEnvelope,
                operation: AssociationEventOperation.Sorted,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnSortingAssociationSubscriptionName,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Association> DoSetAssociationConfidenceAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsNotGloballyBlockedFromContributing(inboundEnvelope.SecurityContext);
            ValidateOnSetAssociationConfidence(association);

            Association storageAssociation =
                await LoadTransitionTargetAsync(
                    associationId: association.Id,
                    securityContext: inboundEnvelope.SecurityContext,
                    cancellationToken: cancellationToken);

            await ValidateUserCanSetStorageAssociationConfidenceAsync(
                storageAssociation: storageAssociation,
                securityContext: inboundEnvelope.SecurityContext);

            // All four IConfidence fields move together. A human correcting a machine score
            // must clear the provenance in the same write, or the row claims a model produced
            // a score a publisher typed — and a retraction targeting that model would then
            // sweep up the human's correction.
            storageAssociation.ConfidenceScore = association.ConfidenceScore;
            storageAssociation.ConfidenceReason = association.ConfidenceReason;
            storageAssociation.SourceBatchId = association.SourceBatchId;
            storageAssociation.ModelVersion = association.ModelVersion;

            return await SaveTransitionAsync(
                association: storageAssociation,
                inboundEnvelope: inboundEnvelope,
                operation: AssociationEventOperation.ConfidenceSet,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationConfidenceSubscriptionName,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Association> DoSetAssociationScopeAsync(
            Guid associationId,
            Scope? entityAScope,
            Scope? entityBScope,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsNotGloballyBlockedFromContributing(inboundEnvelope.SecurityContext);
            ValidateOnSetAssociationScope(associationId, entityAScope, entityBScope);

            Association storageAssociation =
                await LoadTransitionTargetAsync(
                    associationId: associationId,
                    securityContext: inboundEnvelope.SecurityContext,
                    cancellationToken: cancellationToken);

            ValidateUserCanSetStorageAssociationScope(inboundEnvelope.SecurityContext);

            // null leaves the endpoint exactly as stored. Defaulting instead would widen it to
            // AllVersions, because that is enum value 0 - the dangerous direction is the one a
            // non-nullable parameter would pick for a caller who said nothing.
            Scope resolvedEntityAScope = entityAScope ?? storageAssociation.EntityAScope;
            Scope resolvedEntityBScope = entityBScope ?? storageAssociation.EntityBScope;

            ValidateScopeIsApplicableToEndpoints(
                storageAssociation: storageAssociation,
                entityAScope: resolvedEntityAScope,
                entityBScope: resolvedEntityBScope);

            storageAssociation.EntityAScope = resolvedEntityAScope;
            storageAssociation.EntityBScope = resolvedEntityBScope;

            // A scope toggle moves the row's effective id, so it moves the row's position in
            // UX_Associations_Pair and can land on a key another row already holds. "Just
            // toggle a flag" reads like it cannot fail, and it can - so this runs the same
            // duplicate check an add relies on the index for, rather than waiting for the
            // database to raise it.
            await ValidateAssociationPairIsUnoccupiedAsync(
                association: storageAssociation,
                cancellationToken: cancellationToken);

            return await SaveTransitionAsync(
                association: storageAssociation,
                inboundEnvelope: inboundEnvelope,
                operation: AssociationEventOperation.Scoped,
                receiverName: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationScopeSubscriptionName,
                cancellationToken: cancellationToken);
        }

        // Loads the row a transition acts on. Every transition authorizes against what is
        // STORED, so the load has to happen before the authorization decision rather than
        // after it, and the NotFound guard belongs with the load.
        private async ValueTask<Association> LoadTransitionTargetAsync(
            System.Guid associationId,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            Association maybeAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(
                    associationId: associationId,
                    cancellationToken: cancellationToken);

            ValidateStorageAssociation(
                maybeAssociation,
                associationId: associationId);

            // A soft-removed row is a takedown. Transitioning one would approve, publish,
            // reorder or re-score something already withdrawn, and would broadcast a fact
            // about it — approving a tombstone would set IsPublished on a row the reads
            // deliberately hide. Reported as not-found, matching the read posture, so a
            // removed id is not distinguishable from one that never existed.
            ValidateStorageAssociationIsNotDeleted(
                maybeAssociation,
                associationId: associationId);

            ValidateUserIsNotBlockedFromEndpoints(
                securityContext: securityContext,
                firstEntityType: maybeAssociation.EntityAType,
                secondEntityType: maybeAssociation.EntityBType);

            return maybeAssociation;
        }

        // The tail every transition shares: stamp the audit values, save, record the inbound
        // delivery, publish the operation's OWN fact, record the outbound one. Shared so that
        // no transition can quietly publish Modified — there is exactly one publish call for
        // all five and the operation is a parameter.
        private async ValueTask<Association> SaveTransitionAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            AssociationEventOperation operation,
            string receiverName,
            CancellationToken cancellationToken)
        {
            association = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: association,
                    securityContext: inboundEnvelope.SecurityContext);

            Association updatedAssociation =
                await this.storageBroker.UpdateAssociationAsync(
                    association,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedAssociation);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: operation);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: receiverName,
                cancellationToken: cancellationToken);

            return updatedAssociation;
        }

        // Beside the anchor, a half-step away. At the default spacing that IS the midpoint
        // between the anchor and its neighbour, which is why no neighbour read is needed and
        // exactly one row is written.
        private static int ResolveSortOrder(
            Association anchorAssociation,
            SortPosition position)
        {
            // ValidateStorageAnchorAssociation has already refused an anchor with no sort
            // order, so this cannot be null here - the ?? states that for the compiler rather
            // than asserting it with .Value and leaving a warning that hides the invariant.
            int anchorSortOrder = anchorAssociation.SortOrder ?? 0;
            int halfStep = SortOrderStep / 2;

            return position == SortPosition.Before
                ? anchorSortOrder - halfStep
                : anchorSortOrder + halfStep;
        }
    }
}
