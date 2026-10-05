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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    /// <summary>
    /// Coordinates the endpoint-aware association flows that no single foundation service can own,
    /// because the foundation keeps its self-only visibility filter as the dependency-free
    /// primitive and touches only its own entity (design §SEC14.3 Layer, §SEC14.6). It resolves an
    /// association's endpoints against their foundation services, runs the facet gate on the
    /// write (§ARC16.2.1), runs an editorial pair's retrieve-or-add over the unfiltered
    /// canonical-pair probe or hands a reader's reaction to the foundation's personal upsert,
    /// withdraws a reader's reaction by its pair, and returns a status projection that never leaks
    /// the row body.
    ///
    /// <para><b>It is also the layer an exposer binds to for the whole CRUD surface</b>, because
    /// §SEC14.3's composite spans both endpoints and so cannot live in the foundation's own-table
    /// read. The two reads carry that composite — one shared private evaluator, in
    /// <c>.EndpointVisibility.cs</c>. Modify, remove and hard remove carry <b>only</b> the half of
    /// the §SEC14.7 posture A′ gate that needs no row — authentication, the global
    /// <c>ReadOnly</c> block, and <c>Administrators</c> on hard removal — and then forward; every
    /// rule that needs the stored endpoints belongs to the foundation, and no second read
    /// duplicates it. The upsert and the pair-keyed withdrawal resolve both endpoints as their
    /// own first act, and the upsert is the one that decides the endpoint veto for itself, on an
    /// editorial pair — a reader's own reaction is outside it, and it is the only pair the
    /// withdrawal takes.</para>
    ///
    /// <para>Whether the foundation in fact composes each of those from the stored row is its
    /// own business and is not uniform today: on <c>ModifyAssociationAsync</c> the four
    /// <c>ReadOnly</c> names come off the caller's copy ahead of the storage read, which
    /// §SEC14.7 posture A′ rule 4 records as a gap and #658 closes. No member here moves with
    /// it.</para>
    /// </summary>
    internal partial class AssociationOrchestrationService : IAssociationOrchestrationService
    {
        private readonly IAssociationService associationService;
        private readonly IContentItemService contentItemService;
        private readonly ITagService tagService;
        private readonly IReactionService reactionService;
        private readonly IBibleReferenceService bibleReferenceService;
        private readonly ICommentService commentService;
        private readonly ILinkService linkService;
        private readonly IAccessBroker accessBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public AssociationOrchestrationService(
            IAssociationService associationService,
            IContentItemService contentItemService,
            ITagService tagService,
            IReactionService reactionService,
            IBibleReferenceService bibleReferenceService,
            ICommentService commentService,
            ILinkService linkService,
            IAccessBroker accessBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.associationService = associationService;
            this.contentItemService = contentItemService;
            this.tagService = tagService;
            this.reactionService = reactionService;
            this.bibleReferenceService = bibleReferenceService;
            this.commentService = commentService;
            this.linkService = linkService;
            this.accessBroker = accessBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<AssociationSuggestionResult> UpsertAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoUpsertAssociationAsync(
                    association: association,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<AssociationSuggestionResult> DoUpsertAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // The method path's reads are the ambient caller's, which on an HTTP request IS the
            // caller — so no read envelope is carried.
            bool isPersonal = await DeriveAssociationToAddAsync(
                association: association,
                inboundEnvelope: inboundEnvelope,
                readEnvelope: null,
                cancellationToken: cancellationToken);

            return isPersonal
                ? await UpsertPersonalPairAsync(association, cancellationToken)
                : await AddEditorialPairAsync(association, cancellationToken);
        }

        // THE PERSONAL ARM (AssociationOrchestrationService.md §1 rules 4 and 5). A reaction row is
        // created at Submitted, never at Draft (§ARC16.8.1): the seeded personal tier opens the
        // round and closes it on submission, and a row created at Draft would never be counted.
        // The foundation resolves the reader's row itself, after its own canonical ordering, so
        // this arm runs no probe of its own (§ARC16.2.2).
        private async ValueTask<AssociationSuggestionResult> UpsertPersonalPairAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            association.ApprovalStatus = ApprovalStatus.Submitted;

            PersonalAssociationUpsert personalAssociationUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    association,
                    cancellationToken);

            return new AssociationSuggestionResult
            {
                Status = ToSuggestionStatus(personalAssociationUpsert),
                AssociationId = personalAssociationUpsert.Association.Id,
            };
        }

        // The foundation's outcome becomes the result's status (AssociationOrchestrationService.md
        // §1 rule 5), and an unchanged row answers by its status, as the editorial arm answers an
        // occupant. Every outcome is declared, and one that is not is a hard error rather than a
        // default, as EntityTypePersonalisation refuses an undeclared member (§DOM4.10 rule 4): a
        // default would answer an outcome added to the foundation with a status nobody decided.
        private static AssociationSuggestionStatus ToSuggestionStatus(
            PersonalAssociationUpsert personalAssociationUpsert) =>
            personalAssociationUpsert.Outcome switch
            {
                PersonalAssociationUpsertOutcome.Created => AssociationSuggestionStatus.Created,
                PersonalAssociationUpsertOutcome.Restored => AssociationSuggestionStatus.Restored,
                PersonalAssociationUpsertOutcome.Repointed => AssociationSuggestionStatus.Repointed,

                PersonalAssociationUpsertOutcome.Unchanged
                    when personalAssociationUpsert.Association.ApprovalStatus == ApprovalStatus.Approved =>
                        AssociationSuggestionStatus.AlreadyApproved,

                PersonalAssociationUpsertOutcome.Unchanged => AssociationSuggestionStatus.AlreadyPending,

                // a takedown tells the reader nothing about why (§DOM4.10 rule 7)
                PersonalAssociationUpsertOutcome.TakenDown => AssociationSuggestionStatus.AlreadyPending,

                _ => throw new NotSupportedException(
                    $"Personal association upsert outcome '{personalAssociationUpsert.Outcome}' " +
                    "has no declared suggestion status."),
            };

        // THE EDITORIAL ARM: the add as it was, unchanged (§ARC16.8.1) — the two probes, the
        // insert of a free pair and the same statuses. It never repoints.
        private async ValueTask<AssociationSuggestionResult> AddEditorialPairAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            (AssociationPairMatch? existingMatch, AssociationPairMatch? overlappingMatch) =
                await FindPairOccupantsAsync(
                    association: association,
                    readEnvelope: null,
                    cancellationToken: cancellationToken);

            if (existingMatch is null)
            {
                // Nothing on the exact pair, but a differently-scoped live row may overlap it.
                // Report the overlap, insert nothing.
                if (overlappingMatch is not null)
                {
                    return new AssociationSuggestionResult
                    {
                        Status = AssociationSuggestionStatus.OverlapsExisting,
                        AssociationId = overlappingMatch.Id,
                    };
                }

                // the pair is unoccupied and nothing overlaps — insert the new suggestion
                Association addedAssociation =
                    await this.associationService.AddAssociationAsync(
                        association,
                        cancellationToken);

                return new AssociationSuggestionResult
                {
                    Status = AssociationSuggestionStatus.Created,
                    AssociationId = addedAssociation.Id,
                };
            }

            // A soft-deleted row occupies the pair. Whether an editorial row is ever revived is not
            // settled (§ARC16.8.1) — a reader's own reaction is revived on the personal arm, never
            // here — so this arm takes the SAFE branch: it never inserts past a deleted row (which
            // would either duplicate it or launder a takedown), and reports it as already pending,
            // which reveals nothing.
            if (existingMatch.IsDeleted)
            {
                return new AssociationSuggestionResult
                {
                    Status = AssociationSuggestionStatus.AlreadyPending,
                    AssociationId = existingMatch.Id,
                };
            }

            // A live row occupies the pair — return it, insert nothing. Pending and rejected
            // deliberately share the AlreadyPending status so a contributor cannot infer a
            // rejection by resubmitting.
            AssociationSuggestionStatus liveStatus =
                existingMatch.ApprovalStatus == ApprovalStatus.Approved
                    ? AssociationSuggestionStatus.AlreadyApproved
                    : AssociationSuggestionStatus.AlreadyPending;

            return new AssociationSuggestionResult
            {
                Status = liveStatus,
                AssociationId = existingMatch.Id,
            };
        }

        // THE OCCUPANCY CHECK, the second half of the add's write flow and shared by both entry
        // paths in the same way (#631 criterion 4b). The paths differ only in how they ANSWER an
        // occupant — the method path with a status, the event path with a refusal — so only the
        // answer is theirs.
        //
        // First the unfiltered canonical-pair lookup, which sees a pending or rejected row owned
        // by another user, and a soft-deleted one, both of which the caller's read posture hides.
        // Only when no row occupies the EXACT pair is the overlap asked: a differently-scoped
        // LIVE row can still overlap this one's coverage — an AllVersions endpoint spanning a
        // ThisVersionOnly row's version, or the reverse. Their effective ids differ, so the
        // unique index is blind to it, yet inserting past it would render the same pairing twice
        // from two rows with independent approval lifecycles.
        //
        // WHOSE gate each probe asks follows the read envelope, as the endpoint reads do: none on
        // the method path, where the ambient caller is the caller; the inbound one on the event
        // path, so the signed caller is asked (§ARC12.5.2 Rule 3).
        private async ValueTask<(AssociationPairMatch? PairMatch, AssociationPairMatch? OverlappingMatch)>
            FindPairOccupantsAsync(
                Association association,
                EventEnvelope<Association>? readEnvelope,
                CancellationToken cancellationToken)
        {
            AssociationPairMatch? pairMatch = readEnvelope is null
                ? await this.associationService.FindAssociationByPairAsync(
                    association,
                    cancellationToken)
                : await this.associationService.FindAssociationByPairAsync(
                    association,
                    readEnvelope,
                    cancellationToken);

            if (pairMatch is not null)
            {
                return (pairMatch, null);
            }

            AssociationPairMatch? overlappingMatch = readEnvelope is null
                ? await this.associationService.FindOverlappingAssociationAsync(
                    association,
                    excludedAssociationId: null,
                    cancellationToken)
                : await this.associationService.FindOverlappingAssociationAsync(
                    association,
                    readEnvelope,
                    cancellationToken);

            return (null, overlappingMatch);
        }

        // THE ADD'S WRITE FLOW — every rule that decides whether an add may happen and what the
        // row derives to — written ONCE and run by BOTH entry paths: UpsertAssociationAsync on the
        // way to its branch, and the Association-Adding handler on the way to the foundation's
        // own handler (#631). A rule added here is on both doors by construction and cannot be
        // added to one alone, which is what makes "a gate the event path walks past"
        // structurally impossible rather than merely absent today.
        //
        // The two paths differ only in WHOSE reads resolve the endpoints: the method path passes
        // no read envelope and its endpoint services read as the ambient caller; the event path
        // passes the inbound envelope so they read as the signed one.
        //
        // It answers whether the pair is personal, which the method path branches on once the
        // flow is through. The event path never sees a personal pair here, because it refuses
        // one before the flow begins (#723).
        private async ValueTask<bool> DeriveAssociationToAddAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            EventEnvelope<Association>? readEnvelope,
            CancellationToken cancellationToken)
        {
            bool isPersonal = IsPersonalPair(association);

            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext, isPersonal);
            ValidateOnAddAssociation(association);

            // Each resolution is kept for the facet gate.
            (ResolvedEndpoint resolvedEntityA, ResolvedEndpoint resolvedEntityB) =
                await ResolvePairEndpointsAsync(
                    association: association,
                    readEnvelope: readEnvelope,
                    cancellationToken: cancellationToken);

            // The endpoint half of the veto, decidable HERE above the foundation: the add resolves
            // both endpoints from storage as its own first act, so §SEC14.7 posture A′ rule 4's
            // split puts this half on the orchestration rather than below it. Asked before the
            // pair probe, so a blocked caller cannot use the add to learn which pairings already
            // exist. An editorial pair's alone: a reader's own reaction is outside the veto
            // (posture A′ rule 1), which is why the pair-keyed withdrawal, the other write to
            // resolve both endpoints, never asks it.
            if (isPersonal is false)
            {
                ValidateUserIsNotBlockedFromEndpoints(inboundEnvelope.SecurityContext, association);
            }

            // UserId is derived, never the caller's to set (§DOM4.10 rules 1 and 2): the caller's
            // own, from the envelope, on a personal pair, and null on an editorial one, whatever
            // the request carried. It routes the row to one of the two unique indexes and selects
            // its approval tier (§DOM4.10 rule 3), so a value the caller chose would file the row
            // under a constraint that never sees it — and on an editorial pair would evade the
            // canonical-pair probe, laundering an insert past a moderator's takedown.
            association.UserId = isPersonal
                ? inboundEnvelope.SecurityContext.SubjectId
                : null;

            // THE FACET GATE (§ARC16.2.1), last in the flow: after both endpoints resolve and the
            // UserId is derived, and before the method path's pair probe and the event path's
            // claims check, so a refused pair reaches no row through either door.
            await ValidateSettingsAllowTheFacetAsync(
                association,
                resolvedEntityA,
                resolvedEntityB,
                cancellationToken);

            return isPersonal;
        }

        // WHICH PAIR THESE ENDPOINTS DENOTE: both endpoints resolved against their foundation
        // services, and the scope, group id and content type DERIVED onto the row, overwriting
        // anything the caller supplied — the content type is an authorization input and a
        // caller-set scope could claim AllVersions on an entity with no group (§7.4, §5). A
        // non-existent or non-visible endpoint surfaces here as not-found. Written once, for the
        // add's write flow and for the pair-keyed withdrawal, so the two cannot drift apart on
        // which pair a request names (§ARC16.8, "Which pair do these endpoints denote").
        private async ValueTask<(ResolvedEndpoint EntityA, ResolvedEndpoint EntityB)>
            ResolvePairEndpointsAsync(
                Association association,
                EventEnvelope<Association>? readEnvelope,
                CancellationToken cancellationToken)
        {
            ResolvedEndpoint resolvedEntityA = default;
            ResolvedEndpoint resolvedEntityB = default;

            await ResolveEndpointAsync(
                association.EntityAType,
                association.EntityAKeyId,
                onResolved: resolved =>
                {
                    resolvedEntityA = resolved;
                    association.EntityAGroupId = resolved.GroupId;
                    association.EntityAContentType = resolved.ContentType;
                    association.EntityAScope = resolved.Scope;
                },
                endpointName: "A",
                readEnvelope: readEnvelope,
                cancellationToken: cancellationToken);

            await ResolveEndpointAsync(
                association.EntityBType,
                association.EntityBKeyId,
                onResolved: resolved =>
                {
                    resolvedEntityB = resolved;
                    association.EntityBGroupId = resolved.GroupId;
                    association.EntityBContentType = resolved.ContentType;
                    association.EntityBScope = resolved.Scope;
                },
                endpointName: "B",
                readEnvelope: readEnvelope,
                cancellationToken: cancellationToken);

            return (resolvedEntityA, resolvedEntityB);
        }

        // THE FLOW'S PERSONALITY, asked of the RAW endpoint types at its top, because its first
        // step needs it before anything is read (AssociationOrchestrationService.md §1 rule 1). A
        // pair is personal where either endpoint's type is, and that is the lookup's answer, never
        // a test of this service's own (§DOM4.10 rule 4). A type outside the enum is not asked,
        // for IsPersonalEndpoint's reason: the structural validation refuses it next as invalid,
        // where the lookup would throw.
        //
        // The event door asks the same lookup for its own refusal through IsPersonalEndpoint, and
        // the two share no member: a member both the shared flow and an entry path reach is a
        // second route to one of the flow's rules, which the write-flow seam refuses.
        private static bool IsPersonalPair(Association association) =>
            new[] { association.EntityAType, association.EntityBType }
                .Any(entityType =>
                    Enum.IsDefined(entityType)
                    && EntityTypePersonalisation.IsPersonal(entityType));
    }
}
