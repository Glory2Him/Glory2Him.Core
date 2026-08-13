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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
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
    /// primitive and touches only its own entity (design §14.6 layer note). This service resolves
    /// an association's endpoints against their foundation services, runs the retrieve-or-add
    /// suggestion over the unfiltered canonical-pair probe, and returns a status projection that
    /// never leaks the row body.
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
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ILoggingBroker loggingBroker;

        public AssociationOrchestrationService(
            IAssociationService associationService,
            IContentItemService contentItemService,
            ITagService tagService,
            IReactionService reactionService,
            IBibleReferenceService bibleReferenceService,
            ICommentService commentService,
            ILinkService linkService,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ILoggingBroker loggingBroker)
        {
            this.associationService = associationService;
            this.contentItemService = contentItemService;
            this.tagService = tagService;
            this.reactionService = reactionService;
            this.bibleReferenceService = bibleReferenceService;
            this.commentService = commentService;
            this.linkService = linkService;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<AssociationSuggestionResult> AddAssociationAsync(
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

        private async ValueTask<AssociationSuggestionResult> DoAddAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnAddAssociation(association);

            // Resolve BOTH endpoints against their foundation services and DERIVE the scope,
            // group id and content type onto the row, overwriting anything the caller supplied —
            // the content type is an authorization input and a caller-set scope could claim
            // AllVersions on an entity with no group (§7.4, §5). A non-existent or non-visible
            // endpoint surfaces here as not-found.
            await ResolveEndpointAsync(
                association.EntityAType,
                association.EntityAKeyId,
                onResolved: resolved =>
                {
                    association.EntityAGroupId = resolved.GroupId;
                    association.EntityAContentType = resolved.ContentType;
                    association.EntityAScope = resolved.Scope;
                },
                endpointName: "A",
                cancellationToken: cancellationToken);

            await ResolveEndpointAsync(
                association.EntityBType,
                association.EntityBKeyId,
                onResolved: resolved =>
                {
                    association.EntityBGroupId = resolved.GroupId;
                    association.EntityBContentType = resolved.ContentType;
                    association.EntityBScope = resolved.Scope;
                },
                endpointName: "B",
                cancellationToken: cancellationToken);

            // UserId is not the caller's to set. It partitions BOTH the canonical-pair probe and
            // the unique index, so a caller-supplied value would evade the probe — missing a
            // soft-deleted moderator-takedown row and laundering a fresh insert past it, or
            // duplicating a live editorial row. The only rows that legitimately carry a UserId are
            // per-user reactions, whose replace-on-react flow (thread 4) derives it from the caller
            // and does not exist yet; until then every suggestion is editorial and carries no user.
            association.UserId = null;

            // The unfiltered canonical-pair lookup — sees a pending/rejected row owned by another
            // user, and a soft-deleted one, both of which the caller's read posture hides.
            AssociationPairMatch? existingMatch =
                await this.associationService.FindAssociationByPairAsync(
                    association,
                    cancellationToken);

            if (existingMatch is null)
            {
                // the pair is unoccupied — insert the new suggestion
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

            // A soft-deleted row occupies the pair. Resurrecting the caller's own row (and
            // refusing a moderator takedown) is the §10.4 resurrect rule, and it needs a
            // foundation restore primitive that does not exist yet — so this pass takes the SAFE
            // branch: it never inserts past a deleted row (which would either duplicate it or
            // launder a takedown), and reports it as already pending, which reveals nothing.
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
    }
}
