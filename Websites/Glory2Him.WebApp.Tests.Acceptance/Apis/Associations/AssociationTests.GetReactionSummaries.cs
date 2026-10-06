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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.Associations;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    public partial class AssociationApiTests
    {
        [Fact]
        public async Task ShouldServeTheCountsToAnAnonymousCallerAsync()
        {
            // given
            (CoreContentItem reactedContentItem, string _) =
                await ArrangeAnItemTwoReadersReactedToAsync();

            List<ContentItemReactionCount> expectedReactions = CreateOneAmenAndOneLoveCounts();
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetReactionSummariesAsync(
                        CreateContentItemIdsQuery(reactedContentItem.Id));

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<ContentItemReactionSummary> actualSummaries =
                    await ReadSummariesAsync(actualResponse);

                ContentItemReactionSummary actualSummary =
                    actualSummaries.Should().ContainSingle().Subject;

                actualSummary.ContentItemId.Should().Be(reactedContentItem.Id);
                actualSummary.Reactions.Should().BeEquivalentTo(expectedReactions);
                actualSummary.ViewerReactionId.Should().BeNull();
                actualSummary.ViewerReactionName.Should().BeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(reactedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(reactedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldNameTheCallersOwnReactionOverHttpAsync()
        {
            // given
            (CoreContentItem reactedContentItem, string firstReaderId) =
                await ArrangeAnItemTwoReadersReactedToAsync();

            List<ContentItemReactionCount> expectedReactions = CreateOneAmenAndOneLoveCounts();
            this.apiBroker.ActAs(firstReaderId);

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetReactionSummariesAsync(
                        CreateContentItemIdsQuery(reactedContentItem.Id));

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<ContentItemReactionSummary> actualSummaries =
                    await ReadSummariesAsync(actualResponse);

                ContentItemReactionSummary actualSummary =
                    actualSummaries.Should().ContainSingle().Subject;

                actualSummary.ContentItemId.Should().Be(reactedContentItem.Id);
                actualSummary.Reactions.Should().BeEquivalentTo(expectedReactions);
                actualSummary.ViewerReactionId.Should().Be(seededAmenReactionId);
                actualSummary.ViewerReactionName.Should().Be("Amen");
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(reactedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(reactedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldIgnoreQueryOptionsOnReactionSummariesAsync()
        {
            // given
            (CoreContentItem reactedContentItem, string _) =
                await ArrangeAnItemTwoReadersReactedToAsync();

            string contentItemIdsQuery = CreateContentItemIdsQuery(reactedContentItem.Id);

            // Each option, applied, would change the answer: the filter matches no summary, the
            // top keeps none, and the select drops the counts.
            string queryOptions =
                $"$filter={Uri.EscapeDataString($"contentItemId eq {Guid.NewGuid()}")}"
                    + "&$top=0"
                    + "&$select=contentItemId";

            this.apiBroker.ActAsAnonymous();

            try
            {
                HttpResponseMessage expectedResponse =
                    await this.apiBroker.GetReactionSummariesAsync(contentItemIdsQuery);

                string expectedBody = await expectedResponse.Content.ReadAsStringAsync();

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetReactionSummariesAsync(
                        $"{contentItemIdsQuery}&{queryOptions}");

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                string actualBody = await actualResponse.Content.ReadAsStringAsync();
                actualBody.Should().Be(expectedBody);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(reactedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(reactedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldBindRepeatedContentItemIdsFromTheQueryStringAsync()
        {
            // given
            CoreContentItem firstContentItem = await InsertPublishedContentItemAsync();
            CoreContentItem secondContentItem = await InsertPublishedContentItemAsync();

            Guid[] expectedContentItemIds =
                new[] { firstContentItem.Id, secondContentItem.Id };

            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetReactionSummariesAsync(
                        CreateContentItemIdsQuery(firstContentItem.Id, secondContentItem.Id));

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<ContentItemReactionSummary> actualSummaries =
                    await ReadSummariesAsync(actualResponse);

                actualSummaries.Select(summary => summary.ContentItemId)
                    .Should().BeEquivalentTo(expectedContentItemIds);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(firstContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(secondContentItem.Id);
            }
        }
    }
}
