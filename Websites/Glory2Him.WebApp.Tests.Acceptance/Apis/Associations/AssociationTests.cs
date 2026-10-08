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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.Associations;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    [Collection(nameof(ApiTestCollection))]
    public partial class AssociationApiTests
    {
        // Three of the five reactions ReactionSeedData ships, by the deterministic ids it seeds
        // them under. They are configuration rather than fixtures — approved, published, and
        // present in every host — so a test reacts with them rather than arranging its own.
        private static readonly Guid seededAmenReactionId =
            new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000001");

        private static readonly Guid seededLoveReactionId =
            new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000002");

        private static readonly Guid seededJoyReactionId =
            new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000003");

        private readonly ApiBroker apiBroker;

        public AssociationApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        private async ValueTask<CoreContentItem> InsertPublishedContentItemAsync(
            ContentType contentType = ContentType.Story)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return await this.apiBroker.InsertFeedContentItemAsync(
                createdWhen: now,
                publishDate: now,
                contentType: contentType);
        }

        private static Association CreateReactionPair(Guid contentItemId, Guid reactionId) =>
            new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = contentItemId,
                EntityBType = EntityType.Reaction,
                EntityBKeyId = reactionId
            };

        private static async ValueTask<AssociationSuggestionResult> ReadResultAsync(
            HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<AssociationSuggestionResult>();

        /// <summary>
        /// Has two readers react to the item — the first with Amen, the second with Love —
        /// through the upsert route, as a reader gives one. Returns the first reader's id, so a
        /// test can ask as that reader. A test calls it inside the try its teardown closes, so a
        /// post that throws still leaves nothing behind.
        /// </summary>
        private async ValueTask<string> GiveAmenAndLoveFromTwoReadersAsync(Guid contentItemId)
        {
            string firstReaderId = this.apiBroker.ActAsContributor();

            await this.apiBroker.PostAssociationAsync(
                CreateReactionPair(contentItemId, seededAmenReactionId));

            this.apiBroker.ActAsContributor();

            await this.apiBroker.PostAssociationAsync(
                CreateReactionPair(contentItemId, seededLoveReactionId));

            return firstReaderId;
        }

        // ReactionSeedData's two rows, as the summary projects them.
        private static List<ContentItemReactionCount> CreateOneAmenAndOneLoveCounts() =>
            new List<ContentItemReactionCount>
            {
                new ContentItemReactionCount
                {
                    ReactionId = seededAmenReactionId,
                    Name = "Amen",
                    UnicodeEmoji = "👍",
                    Count = 1
                },

                new ContentItemReactionCount
                {
                    ReactionId = seededLoveReactionId,
                    Name = "Love",
                    UnicodeEmoji = "❤️",
                    Count = 1
                }
            };

        // Each id as its own contentItemIds parameter, which is how a Guid[] binds from a query
        // string.
        private static string CreateContentItemIdsQuery(params Guid[] contentItemIds) =>
            string.Join("&", contentItemIds.Select(contentItemId => $"contentItemIds={contentItemId}"));

        private static async ValueTask<List<ContentItemReactionSummary>> ReadSummariesAsync(
            HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<List<ContentItemReactionSummary>>();
    }
}
