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
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

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
        public async Task ShouldSuggestAnEditorialPairingOverHttpAsync()
        {
            // given
            // The seeded Story default allows tags (ContentItemSettingSeedData), and a draft tag
            // is visible to its author, so neither the facet gate nor endpoint resolution turns
            // the pair away. The body carries the two endpoints and no Id, as the client sends it.
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string contributorUserId = this.apiBroker.ActAsContributor();
            CoreTag contributorsTag = await this.apiBroker.InsertDraftTagAsync(contributorUserId);
            Association inputPair = CreateTagPair(publishedContentItem.Id, contributorsTag.Id);

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                AssociationSuggestionResult actualResult = await ReadResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
                actualResult.AssociationId.Should().NotBeEmpty();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagByIdAsync(contributorsTag.Id);
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
        public async Task ShouldAnswerAReactionCarryingOperationOwnedFieldsAsWithoutThemOverHttpAsync()
        {
            // given
            // A field that got past the upsert's working copy would earn the create arm's 400
            // (#1094). That is the only proof of the bypass pair's drop: the reaction is approved
            // inside its own request, and the approval writes the pair from the access decision
            // (AssociationOrchestrationService.md §6 rule 6).
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            string inputBody = CreateBodyCarryingOperationOwnedFields(inputPair);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostAssociationRawAsync(inputBody);

                AssociationSuggestionResult actualResult = await ReadResultAsync(actualResponse);

                // then: the answer ShouldGiveAReactionOverHttpAsync gets for the bare pair
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
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
        public async Task ShouldStoreNoneOfTheDroppedFieldsOnAReactionOverHttpAsync()
        {
            // given
            // The post ShouldAnswerAReactionCarryingOperationOwnedFieldsAsWithoutThemOverHttpAsync
            // makes. No confidence process is built, so nothing but this post could write the five
            // fields the stored row is read for (AssociationOrchestrationService.md §6 rule 6).
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            string inputBody = CreateBodyCarryingOperationOwnedFields(inputPair);
            this.apiBroker.ActAsContributor();

            try
            {
                HttpResponseMessage postResponse =
                    await this.apiBroker.PostAssociationRawAsync(inputBody);

                AssociationSuggestionResult postResult = await ReadResultAsync(postResponse);
                this.apiBroker.ActAsSeededAdministrator();

                // when
                CoreAssociation storedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(postResult.AssociationId);

                // then
                storedAssociation.Should().NotBeNull();
                storedAssociation.SortOrder.Should().BeNull();
                storedAssociation.ConfidenceScore.Should().BeNull();
                storedAssociation.ConfidenceReason.Should().BeNull();
                storedAssociation.SourceBatchId.Should().BeNull();
                storedAssociation.ModelVersion.Should().BeNull();
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

                string actualBody = await actualResponse.Content.ReadAsStringAsync();

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // The attribute's challenge answers with no body. The orchestration's own refusal
                // also maps to 401, but carries a problem-details body, so an empty body is what
                // shows the attribute turned the caller away before the orchestration was asked.
                actualBody.Should().BeEmpty();
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
