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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        public async ValueTask<IReadOnlyList<ContentItemReactionSummary>> RetrieveContentItemReactionSummariesAsync(
            IReadOnlyList<Guid> contentItemIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PublicContentItemGroup> hosts =
                await this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds,
                    cancellationToken);

            IReadOnlyList<Reaction> vocabulary =
                await this.reactionService.RetrievePublicReactionsAsync(cancellationToken);

            IReadOnlyList<AssociationPairCount> pairCounts =
                await this.associationService.RetrieveContentItemReactionCountsAsync(
                    hosts.Select(host => host.GroupId).ToList(),
                    vocabulary.Select(reaction => reaction.Id).ToList(),
                    cancellationToken);

            return hosts
                .Select(host => new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = CountReactionsGivenTo(host, vocabulary, pairCounts),
                })
                .ToList();
        }

        // The host's counts in the vocabulary's order, the order the vocabulary read answers in,
        // whatever order the counts arrive in: it is walked rather than sorted.
        private static List<ContentItemReactionCount> CountReactionsGivenTo(
            PublicContentItemGroup host,
            IReadOnlyList<Reaction> vocabulary,
            IReadOnlyList<AssociationPairCount> pairCounts) =>
            vocabulary
                .Select(reaction => new
                {
                    Reaction = reaction,

                    PairCount = pairCounts.FirstOrDefault(pairCount =>
                        pairCount.EntityAEffectiveId == host.GroupId
                            && pairCount.EntityBKeyId == reaction.Id),
                })
                .Where(reactionCount => reactionCount.PairCount is not null)
                .Select(reactionCount => new ContentItemReactionCount
                {
                    ReactionId = reactionCount.Reaction.Id,
                    Name = reactionCount.Reaction.Name,
                    UnicodeEmoji = reactionCount.Reaction.UnicodeEmoji,
                    Count = reactionCount.PairCount!.Count,
                })
                .ToList();
    }
}
