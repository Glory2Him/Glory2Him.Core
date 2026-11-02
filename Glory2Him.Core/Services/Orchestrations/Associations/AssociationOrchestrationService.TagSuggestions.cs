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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        public async ValueTask<AssociationSubmissionResult> SuggestTagAsync(
            TagSuggestion tagSuggestion,
            CancellationToken cancellationToken = default)
        {
            EventEnvelope<TagSuggestion> suggestionEnvelope =
                await this.eventEnvelopeBroker.CreateAsync(content: tagSuggestion);

            var suggestedPair = new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = tagSuggestion.ContentItemId,
                EntityBType = EntityType.Tag,
                EntityBScope = EntityTypeVersioning.DefaultScopeFor(EntityType.Tag),
            };

            await ResolveEndpointAsync(
                suggestedPair.EntityAType,
                suggestedPair.EntityAKeyId,
                onResolved: resolved =>
                {
                    suggestedPair.EntityAGroupId = resolved.GroupId;
                    suggestedPair.EntityAContentType = resolved.ContentType;
                    suggestedPair.EntityAScope = resolved.Scope;
                },
                endpointName: "A",
                readEnvelope: null,
                cancellationToken: cancellationToken);

            TagNameResult tagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    new Tag
                    {
                        Name = tagSuggestion.Name.Trim(),
                        ApprovalStatus = ApprovalStatus.Submitted,
                    },
                    cancellationToken);

            suggestedPair.EntityBKeyId = tagNameResult.Id;
            suggestedPair.EntityBGroupId = tagNameResult.Id;

            if (tagNameResult.Outcome is not TagNameOutcome.Added)
            {
                await ReadTheTagAsTheCallerAsync(tagNameResult.Id, cancellationToken);
            }

            (AssociationPairMatch? pairMatch, AssociationPairMatch? overlappingMatch) =
                await FindPairOccupantsAsync(
                    association: suggestedPair,
                    readEnvelope: null,
                    cancellationToken: cancellationToken);

            AssociationPairMatch? occupant = pairMatch ?? overlappingMatch;

            if (occupant is not null)
            {
                return new AssociationSubmissionResult
                {
                    Status = ToSubmissionStatus(occupant, suggestionEnvelope.SecurityContext),
                };
            }

            suggestedPair.ApprovalStatus = ApprovalStatus.Submitted;

            await this.associationService.AddAssociationAsync(
                suggestedPair,
                cancellationToken);

            return new AssociationSubmissionResult
            {
                Status = AssociationSubmissionStatus.Created,
            };
        }

        private static AssociationSubmissionStatus ToSubmissionStatus(
            AssociationPairMatch occupant,
            SecurityContext securityContext)
        {
            bool isTheCallersOwn = string.Equals(
                occupant.CreatedBy,
                securityContext.SubjectId,
                StringComparison.Ordinal);

            if (isTheCallersOwn is false)
            {
                return AssociationSubmissionStatus.AlreadySuggested;
            }

            bool isPending = occupant.IsDeleted is false
                && occupant.ApprovalStatus is ApprovalStatus.Draft or ApprovalStatus.Submitted;

            return isPending
                ? AssociationSubmissionStatus.AlreadySuggestedByCaller
                : AssociationSubmissionStatus.PreviouslySuggestedByCaller;
        }

        private async ValueTask ReadTheTagAsTheCallerAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.tagService.RetrieveTagByIdAsync(tagId, cancellationToken);
            }
            catch (TagValidationException)
            {
            }
        }
    }
}
