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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Reactions
{
    /// <summary>
    /// Proves the order a reaction is presented in reaches the database — the column, its
    /// default, and the backfill that gives the five seeded reactions their order on a
    /// database that booted before the column existed.
    ///
    /// <para>A seeded <c>0</c> surviving the insert and a raw-SQL insert taking the column
    /// default are not re-proven here: <c>ContentItemSettingSortOrderTests</c> proves that
    /// mechanism for this shape, and <c>StorageBrokerStoreDefaultTests</c> guards the
    /// configuration.</para>
    /// </summary>
    [Collection(ReactionCollection.Name)]
    public sealed class ReactionSortOrderTests : IDisposable
    {
        private readonly ReactionQueryBroker broker;
        private readonly List<Guid> seededReactionIds;

        public ReactionSortOrderTests(ReactionQueryBroker broker)
        {
            this.broker = broker;
            this.seededReactionIds = new List<Guid>();
        }

        [Fact]
        public async Task ShouldStoreTheSortOrderTheEntityCarriesWhenNothingSetsItAsync()
        {
            // given: the entity's own default matches the column's, so a reaction nobody
            // ordered lands past the curated seed values rather than in front of them.
            Reaction unorderedReaction = ReactionQueryBroker.CreateReaction();
            this.seededReactionIds.Add(unorderedReaction.Id);

            // when
            await this.broker.InsertAsync(unorderedReaction);

            int storedSortOrder =
                await this.broker.GetStoredSortOrderAsync(unorderedReaction.Id);

            // then
            storedSortOrder.Should().Be(1000,
                because: "an unordered reaction sorts after every one somebody chose the order of");
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededReactionIds)
                .AsTask().GetAwaiter().GetResult();
    }
}
