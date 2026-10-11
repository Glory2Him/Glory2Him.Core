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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    internal partial class LiveUpdateOrchestrationService
    {
        // THE REACTIONS BODY, written once for every reaction fact the forwarder hears
        // (LiveUpdateOrchestrationService.md, the reactions body).
        private async ValueTask SendReactionsChangeAsync(
            Association association,
            CancellationToken cancellationToken)
        {
            if (IsAContentItemsReaction(association) is false)
            {
                return;
            }

            PublicContentItemGroup? visibleVersion =
                await this.contentItemService.FindPublicContentItemGroupAsync(
                    association.EntityAGroupId,
                    cancellationToken);

            // a group nobody may see shows no counts, so nobody may be told they moved
            if (visibleVersion is null)
            {
                return;
            }

            ContentItemSettingKey settingKey = CreateSettingKeyFor(visibleVersion);

            IReadOnlyList<EffectiveContentItemSetting> winningSettings =
                await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                    new List<ContentItemSettingKey> { settingKey },
                    cancellationToken);

            // the version's counts are shown only where its winning setting shows reactions, and
            // a setting that cannot be resolved never means "show": the decision fails closed
            // (§SEC14.8 rule 3)
            if (FindWinningSetting(winningSettings, settingKey)?.ShowReactions is not true)
            {
                return;
            }

            await this.liveUpdateService.SendLiveUpdateAsync(
                new LiveUpdate
                {
                    Type = LiveUpdateType.ContentItemReactionsChanged,
                    ContentItemId = visibleVersion.ContentItemId,
                },
                cancellationToken);
        }

        // The pair §ARC16.8's predicate counts: endpoint A a content item, endpoint B a reaction.
        // No other row can move a count a card shows.
        private static bool IsAContentItemsReaction(Association association) =>
            association.EntityAType == EntityType.ContentItem
                && association.EntityBType == EntityType.Reaction;

        // §DOM6.4's key for the version: its own content type and its own id.
        private static ContentItemSettingKey CreateSettingKeyFor(
            PublicContentItemGroup visibleVersion) =>
            new ContentItemSettingKey
            {
                ContentType = visibleVersion.ContentType,
                ContentItemId = visibleVersion.ContentItemId,
            };

        // The answer is per key, one item under one content type, and a key that resolves no row
        // is absent from it (IAccessBroker), so the version takes the row for its own key only.
        private static ContentItemSetting? FindWinningSetting(
            IReadOnlyList<EffectiveContentItemSetting> winningSettings,
            ContentItemSettingKey settingKey) =>
            winningSettings
                .Where(winningSetting =>
                    winningSetting.ContentItemId == settingKey.ContentItemId
                        && winningSetting.ContentItemSetting.ContentType == settingKey.ContentType)
                .Select(winningSetting => winningSetting.ContentItemSetting)
                .FirstOrDefault();
    }
}
