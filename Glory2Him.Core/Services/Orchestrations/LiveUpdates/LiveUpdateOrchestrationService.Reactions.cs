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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
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

            await this.accessBroker.RetrieveEffectiveContentItemSettingsAsync(
                new List<ContentItemSettingKey> { CreateSettingKeyFor(visibleVersion) },
                cancellationToken);

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
    }
}
