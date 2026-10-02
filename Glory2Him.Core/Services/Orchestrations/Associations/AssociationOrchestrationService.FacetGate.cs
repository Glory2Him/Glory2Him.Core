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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // A switch the facet gate asks of a winning setting: its name, which a refusal carries,
        // and how to read it.
        private readonly struct FacetSwitch
        {
            public FacetSwitch(string name, Func<ContentItemSetting, bool> isAllowed)
            {
                Name = name;
                IsAllowed = isAllowed;
            }

            public string Name { get; }
            public Func<ContentItemSetting, bool> IsAllowed { get; }
        }

        // One question the gate asks of a pair, in one orientation: the ContentItem host's
        // winning setting, keyed on the host's key id and derived content type, the switch the
        // far end's type names on it, and the far end's reaction name for the narrowing.
        private readonly struct FacetQuestion
        {
            public FacetQuestion(
                ContentItemSettingKey settingKey,
                FacetSwitch facetSwitch,
                string? farEndReactionName)
            {
                SettingKey = settingKey;
                FacetSwitch = facetSwitch;
                FarEndReactionName = farEndReactionName;
            }

            public ContentItemSettingKey SettingKey { get; }
            public FacetSwitch FacetSwitch { get; }
            public string? FarEndReactionName { get; }
        }

        private const string LoveReactionName = "Love";

        // THE §DOM6.10 FACET GATE (§ARC16.2.1; AssociationOrchestrationService.md §1 rule 2). In
        // each orientation the host names the settings entity and the far end names the switch
        // asked of it, and every question is answered by one read of the winning settings.
        private async ValueTask ValidateSettingsAllowTheFacetAsync(
            Association association,
            ResolvedEndpoint resolvedEntityA,
            ResolvedEndpoint resolvedEntityB,
            CancellationToken cancellationToken)
        {
            List<FacetQuestion> facetQuestions =
                CreateFacetQuestions(association, resolvedEntityA, resolvedEntityB);

            IReadOnlyList<EffectiveContentItemSetting> effectiveSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    facetQuestions
                        .Select(facetQuestion => facetQuestion.SettingKey)
                        .ToList(),
                    cancellationToken);

            foreach (FacetQuestion facetQuestion in facetQuestions)
            {
                ContentItemSetting winningSetting = effectiveSettings.Single().ContentItemSetting;

                ValidateSettingAllowsTheFacet(winningSetting, facetQuestion);
            }
        }

        // Both orientations are asked, because the flow never reorders the pair: canonical order is
        // the foundation's to restore (§DOM4.4 rule 4), so the item may be either endpoint.
        private static List<FacetQuestion> CreateFacetQuestions(
            Association association,
            ResolvedEndpoint resolvedEntityA,
            ResolvedEndpoint resolvedEntityB)
        {
            var facetQuestions = new List<FacetQuestion>();

            AddFacetQuestion(
                facetQuestions,
                hostType: association.EntityAType,
                hostKeyId: association.EntityAKeyId,
                hostContentType: association.EntityAContentType,
                farEndType: association.EntityBType,
                farEndReactionName: resolvedEntityB.ReactionName);

            AddFacetQuestion(
                facetQuestions,
                hostType: association.EntityBType,
                hostKeyId: association.EntityBKeyId,
                hostContentType: association.EntityBContentType,
                farEndType: association.EntityAType,
                farEndReactionName: resolvedEntityA.ReactionName);

            return facetQuestions;
        }

        // Only a ContentItem host is asked: it is the one host whose settings entity exists. A
        // BibleReference host's is not built, so no facet on it is gated yet (§ARC16.2.1), and a
        // Tag or a Reaction has none by design (§DOM6.10 rule 3). Its content type is the one
        // resolution derived, which a ContentItem endpoint always carries here.
        private static void AddFacetQuestion(
            List<FacetQuestion> facetQuestions,
            EntityType hostType,
            Guid hostKeyId,
            ContentType? hostContentType,
            EntityType farEndType,
            string? farEndReactionName)
        {
            FacetSwitch? facetSwitch = FindFacetSwitch(farEndType);

            if (hostType is not EntityType.ContentItem || facetSwitch is null)
            {
                return;
            }

            facetQuestions.Add(new FacetQuestion(
                settingKey: new ContentItemSettingKey
                {
                    ContentType = hostContentType!.Value,
                    ContentItemId = hostKeyId,
                },
                facetSwitch: facetSwitch.Value,
                farEndReactionName: farEndReactionName));
        }

        // §ARC16.2.1's table: the switch each far end's type names on a ContentItem host. There is
        // no Attachment arm. No Attachment endpoint resolves (ResolveEndpointCoreAsync refuses it),
        // so no pair reaches one, and the change that first resolves one adds the arm and its test
        // (AssociationOrchestrationService.md §1 rule 2). A far end with no arm asks nothing: a
        // ContentItem far end, as in series membership, is not gated in that orientation.
        private static FacetSwitch? FindFacetSwitch(EntityType farEndType) =>
            farEndType switch
            {
                EntityType.Tag => new FacetSwitch(
                    nameof(ContentItemSetting.TagsAllowed),
                    setting => setting.TagsAllowed),

                EntityType.Reaction => new FacetSwitch(
                    nameof(ContentItemSetting.ReactionsAllowed),
                    setting => setting.ReactionsAllowed),

                EntityType.Comment => new FacetSwitch(
                    nameof(ContentItemSetting.CommentsAllowed),
                    setting => setting.CommentsAllowed),

                EntityType.BibleReference => new FacetSwitch(
                    nameof(ContentItemSetting.BibleReferenceAllowed),
                    setting => setting.BibleReferenceAllowed),

                EntityType.Link => new FacetSwitch(
                    nameof(ContentItemSetting.LinksAllowed),
                    setting => setting.LinksAllowed),

                _ => null,
            };

        // Each refusal names the switch that refused (§ARC16.2.1). §SEC14.5's no-existence-leak
        // posture does not reach it: resolution has already shown the caller both endpoints, so
        // the name discloses a policy on a row they can see.
        private static void ValidateSettingAllowsTheFacet(
            ContentItemSetting winningSetting,
            FacetQuestion facetQuestion) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",

                (Rule: IsNotAllowedBy(winningSetting, facetQuestion.FacetSwitch),
                    Parameter: facetQuestion.FacetSwitch.Name),

                (Rule: IsNotLoveOnALoveOnlyItem(winningSetting, facetQuestion.FarEndReactionName),
                    Parameter: nameof(ContentItemSetting.LimitReactionsToLoveOnly)));

        private static dynamic IsNotAllowedBy(
            ContentItemSetting winningSetting,
            FacetSwitch facetSwitch) => new
            {
                Condition = facetSwitch.IsAllowed(winningSetting) is false,
                Message = "Value does not allow this association"
            };

        // The narrowing (§ARC16.2.1): an item limited to Love admits a reaction by its Name.
        private static dynamic IsNotLoveOnALoveOnlyItem(
            ContentItemSetting winningSetting,
            string? reactionName) => new
            {
                Condition = winningSetting.LimitReactionsToLoveOnly
                    && reactionName != LoveReactionName,

                Message = "Value allows only the Love reaction"
            };
    }
}
