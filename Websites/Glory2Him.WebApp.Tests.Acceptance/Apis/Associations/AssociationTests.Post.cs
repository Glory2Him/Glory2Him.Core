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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using AssociationSuggestionStatus = Glory2Him.Core.Models.Orchestrations.Associations.AssociationSuggestionStatus;
using Glory2Him.WebApp.Tests.Acceptance.Models.Associations;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    public partial class AssociationApiTests
    {
        [Fact]
        public async Task ShouldGiveAReactionOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            AssociationSuggestionResult actualResult = null;
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                actualResult = await ReadResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (actualResult is not null)
                {
                    await this.apiBroker.RemoveCoreAssociationByIdAsync(actualResult.AssociationId);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldChangeAReactionOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association givenPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededLoveReactionId);
            AssociationSuggestionResult givenResult = null;
            this.apiBroker.ActAsContributor();

            try
            {
                HttpResponseMessage givenResponse =
                    await this.apiBroker.PostAssociationAsync(givenPair);

                givenResult = await ReadResultAsync(givenResponse);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                AssociationSuggestionResult actualResult = await ReadResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                actualResult.Status.Should().Be(AssociationSuggestionStatus.Repointed);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (givenResult is not null)
                {
                    await this.apiBroker.RemoveCoreAssociationByIdAsync(givenResult.AssociationId);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldKeepTheHeldReactionOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            AssociationSuggestionResult givenResult = null;
            this.apiBroker.ActAsContributor();

            try
            {
                HttpResponseMessage givenResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                givenResult = await ReadResultAsync(givenResponse);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                AssociationSuggestionResult actualResult = await ReadResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                actualResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyApproved);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (givenResult is not null)
                {
                    await this.apiBroker.RemoveCoreAssociationByIdAsync(givenResult.AssociationId);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }
    }
}
