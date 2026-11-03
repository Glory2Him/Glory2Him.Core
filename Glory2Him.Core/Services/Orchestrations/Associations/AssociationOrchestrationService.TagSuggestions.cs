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
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // THE TAG SUGGESTION (AssociationOrchestrationService.TagSuggestions.md §1). A third way in
        // beside the upsert and the Association-Adding door, and neither of them: it reaches the
        // rules that decide a pairing through the members that hold them — the post's resolution,
        // the endpoint veto, the refusal of a rejected endpoint, the facet gate and the occupancy
        // check — and copies none of them, and neither door calls it. The tag is the tag
        // foundation's to find or add (TagService.TagSuggestions.md §3), so this member reads no
        // tag by name and adds none itself.
        public ValueTask<AssociationSubmissionResult> SuggestTagAsync(
            TagSuggestion tagSuggestion,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagSuggestionIsNotNull(tagSuggestion);

                // Every gate below reads the caller from this envelope, never from an ambient
                // accessor (§ARC12.5.2 rule 6).
                EventEnvelope<TagSuggestion> suggestionEnvelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: tagSuggestion);

                ValidateUserMaySuggestTag(suggestionEnvelope.SecurityContext);

                // Every step below reads the trimmed name, and nothing else of it changes: no hash
                // is stripped and its letter case is the column's (TagSuggestions.md rule 2).
                string? tagName = tagSuggestion.Name?.Trim();
                ValidateOnSuggestTag(tagSuggestion, tagName);

                // THE PAIR, editorial and in canonical order (§DOM4.4): the post on A, derived by
                // its resolution below, and the tag on B, keyed on its id once the find-or-add
                // answers it, at the scope a non-versioned endpoint derives. UserId stays null.
                var suggestedPair = new Association
                {
                    EntityAType = EntityType.ContentItem,
                    EntityAKeyId = tagSuggestion.ContentItemId,
                    EntityBType = EntityType.Tag,
                    EntityBScope = EntityTypeVersioning.DefaultScopeFor(EntityType.Tag),
                };

                // The post is read as the caller, as the upsert resolves its endpoint A, so a post
                // that does not exist and one the caller may not see are both "The A endpoint was
                // not found." (§SEC14.5 rule 1). The resolution is kept for the refusal of a
                // rejected post and for the facet gate, so the post is read once.
                ResolvedEndpoint resolvedPost = default;

                await ResolveEndpointAsync(
                    suggestedPair.EntityAType,
                    suggestedPair.EntityAKeyId,
                    onResolved: resolved =>
                    {
                        resolvedPost = resolved;
                        suggestedPair.EntityAGroupId = resolved.GroupId;
                        suggestedPair.EntityAContentType = resolved.ContentType;
                        suggestedPair.EntityAScope = resolved.Scope;
                    },
                    endpointName: "A",
                    readEnvelope: null,
                    cancellationToken: cancellationToken);

                // The pairing's veto, over the post's derived type and the tag's type. It covers the
                // tag add's own veto, so a caller who could not add the tag is refused before any
                // tag is read (TagSuggestions.md rule 9, step 4).
                ValidateUserIsNotBlockedFromEndpoints(
                    suggestionEnvelope.SecurityContext,
                    suggestedPair);

                ValidateEndpointIsNotRejected(resolvedPost, endpointName: "A");

                // The post is the only host, and the tag on B is a type and nothing more until the
                // find-or-add answers it, so no resolution of it is handed to the gate: a Tag host
                // is asked nothing, and only a reaction's name is read of a far end.
                await ValidateSettingsAllowTheFacetAsync(
                    suggestedPair,
                    resolvedEntityA: resolvedPost,
                    resolvedEntityB: default,
                    cancellationToken);

                TagNameResult tagNameResult =
                    await this.tagService.FindOrAddTagAsync(
                        new Tag
                        {
                            Name = tagName!,
                            ApprovalStatus = ApprovalStatus.Submitted,
                        },
                        cancellationToken);

                suggestedPair.EntityBKeyId = tagNameResult.Id;
                suggestedPair.EntityBGroupId = tagNameResult.Id;

                // WHETHER THE CALLER CAN SEE THE TAG decides the answer, and never whether it is
                // paired: that is decided by the find-or-add's read and the probes, which no
                // identity filters. A tag the find-or-add has just added is the caller's own, and
                // is not read again (TagSuggestions.md rule 5).
                bool isTagVisible =
                    tagNameResult.Outcome is TagNameOutcome.Added
                    || await IsTagVisibleToTheCallerAsync(tagNameResult.Id, cancellationToken);

                // The editorial arm's occupancy check, a withheld tag's pair included, so that its
                // answer tells it from no other tag (TagSuggestions.md rules 4 and 6).
                (AssociationPairMatch? pairMatch, AssociationPairMatch? overlappingMatch) =
                    await FindPairOccupantsAsync(
                        association: suggestedPair,
                        readEnvelope: null,
                        cancellationToken: cancellationToken);

                AssociationPairMatch? occupant = pairMatch ?? overlappingMatch;

                // A free pair is inserted whether or not the caller can see the tag, and a withheld
                // tag is never paired: the find-or-add has logged why, so nothing is thrown or
                // logged for it here.
                if (occupant is null && tagNameResult.Outcome is not TagNameOutcome.Withheld)
                {
                    await AddSuggestedPairAsync(suggestedPair, isTagVisible, cancellationToken);
                }

                // THE ANSWER, a status and no id. Whose a pairing is, is told only of a tag the
                // caller can see: every other answers Created, whatever holds the pair, so nothing
                // tells a tag the caller cannot see from one they created (TagSuggestions.md
                // rules 5 and 7).
                return new AssociationSubmissionResult
                {
                    Status = occupant is not null && isTagVisible
                        ? ToSubmissionStatus(occupant, suggestionEnvelope.SecurityContext)
                        : AssociationSubmissionStatus.Created,
                };
            });

        // THE INSERT OF A FREE PAIR, at Submitted, as the editorial arm inserts one (§ARC16.8.1).
        // The add refuses a pair another reader's insert took between the probes and this one,
        // and for a tag the caller cannot see that refusal is a step of the flow, answered Created
        // like every other suggestion of such a tag (TagSuggestions.md rule 10, "Two readers
        // pairing one tag at once"). It is no catch arm: every other refusal or failure of the add,
        // and this one for a tag the caller can see, reaches the TryCatch.
        private async ValueTask AddSuggestedPairAsync(
            Association suggestedPair,
            bool isTagVisible,
            CancellationToken cancellationToken)
        {
            suggestedPair.ApprovalStatus = ApprovalStatus.Submitted;

            try
            {
                await this.associationService.AddAssociationAsync(
                    suggestedPair,
                    cancellationToken);
            }
            catch (AssociationDependencyValidationException associationDependencyValidationException)
                when (isTagVisible is false
                    && associationDependencyValidationException.InnerException is AlreadyExistsAssociationException)
            {
                // the pair is held, as the caller asked, by a row the answer must not tell of
            }
        }

        // Whose the row a probe found is, its CreatedBy against the envelope's subject, the id the
        // security-audit broker stamps as CreatedBy, compared ordinally as the foundations compare
        // it. The caller's own row is pending at Draft or Submitted while live, and otherwise no
        // longer pending, which never says whether it was approved, rejected or removed.
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

        // The tag's own read, as the ambient caller of this direct call, as endpoint resolution
        // reads a tag endpoint. It answers not found for a row the caller may not see and for every
        // soft-deleted row (§SEC14.7 posture A rule 4; §SEC14.5 rules 1 and 3), and that not-found
        // is an answer, not a refusal. Any other exception of the read reaches the TryCatch.
        private async ValueTask<bool> IsTagVisibleToTheCallerAsync(
            Guid tagId,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.tagService.RetrieveTagByIdAsync(tagId, cancellationToken);

                return true;
            }
            catch (TagValidationException tagValidationException)
                when (tagValidationException.InnerException is NotFoundTagException)
            {
                return false;
            }
        }
    }
}
