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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
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
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                AssociationSuggestionResult actualResult = await ReadResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);

                // Created is the enum's default, so the status alone would also pass on a body
                // that carries no result at all; the id is what a missing result cannot fake.
                actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
                actualResult.AssociationId.Should().NotBeEmpty();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
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
            this.apiBroker.ActAsContributor();

            try
            {
                await this.apiBroker.PostAssociationAsync(givenPair);

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
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldKeepTheHeldReactionOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            this.apiBroker.ActAsContributor();

            try
            {
                await this.apiBroker.PostAssociationAsync(inputPair);

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
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnPostIfTheItemRefusesReactionsAsync()
        {
            // given
            // The seeded Series default refuses reactions (ContentItemSettingSeedData), so the
            // facet gate turns the pair away.
            CoreContentItem publishedSeries =
                await InsertPublishedContentItemAsync(ContentType.Series);

            Association inputPair = CreateReactionPair(publishedSeries.Id, seededAmenReactionId);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedSeries.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostIfTheItemDoesNotExistAsync()
        {
            // given
            Guid nonExistentContentItemId = Guid.NewGuid();
            Association inputPair = CreateReactionPair(nonExistentContentItemId, seededAmenReactionId);
            this.apiBroker.ActAsContributor();

            // when
            HttpResponseMessage actualResponse =
                await this.apiBroker.PostAssociationAsync(inputPair);

            // then
            actualResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
