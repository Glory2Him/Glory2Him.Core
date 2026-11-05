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
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.WebApp.Tests.Acceptance.Models.Associations;
using AssociationSubmissionResult = Glory2Him.WebApp.Tests.Acceptance.Models.Associations.AssociationSubmissionResult;
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;
using TagSuggestion = Glory2Him.WebApp.Tests.Acceptance.Models.Associations.TagSuggestion;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    public partial class AssociationApiTests
    {
        [Fact]
        public async Task ShouldSuggestANewTagOverHttpAsync()
        {
            // given
            // The seeded Story default allows tags (ContentItemSettingSeedData), so the facet
            // gate does not turn the suggestion away. The body carries the post's id and the name
            // and nothing else, as the React request sends it.
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string contributorUserId = this.apiBroker.ActAsContributor();
            string inputName = CreateUnheldTagName();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, inputName);

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                AssociationSubmissionResult actualResult =
                    await ReadSubmissionResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSubmissionStatus.Created);

                IReadOnlyList<CoreTag> storedTags =
                    await this.apiBroker.GetCoreTagsByNameAsync(inputName);

                CoreTag storedTag = storedTags.Should().ContainSingle().Subject;
                storedTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                storedTag.CreatedBy.Should().Be(contributorUserId);

                IReadOnlyList<CoreAssociation> storedPairings =
                    await this.apiBroker.GetCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                CoreAssociation storedPairing = storedPairings.Should().ContainSingle().Subject;
                storedPairing.EntityBType.Should().Be(EntityType.Tag);
                storedPairing.EntityBKeyId.Should().Be(storedTag.Id);
                storedPairing.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(inputName);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldAnswerTheCallersOwnPendingSuggestionOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            this.apiBroker.ActAsContributor();
            string inputName = CreateUnheldTagName();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, inputName);

            try
            {
                await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                AssociationSubmissionResult actualResult =
                    await ReadSubmissionResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                actualResult.Status.Should().Be(AssociationSubmissionStatus.AlreadySuggestedByCaller);

                IReadOnlyList<CoreTag> storedTags =
                    await this.apiBroker.GetCoreTagsByNameAsync(inputName);

                IReadOnlyList<CoreAssociation> storedPairings =
                    await this.apiBroker.GetCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                storedTags.Should().ContainSingle();
                storedPairings.Should().ContainSingle();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(inputName);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldPairATagTheCallerCannotSeeOverHttpAsync()
        {
            // given
            // A draft tag is visible to its author alone, so the contributor cannot see it.
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string otherUserId = this.apiBroker.ActAsContributor();
            CoreTag othersTag = await this.apiBroker.InsertDraftTagAsync(otherUserId);
            this.apiBroker.ActAsContributor();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, othersTag.Name);

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                AssociationSubmissionResult actualResult =
                    await ReadSubmissionResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSubmissionStatus.Created);

                IReadOnlyList<CoreTag> storedTags =
                    await this.apiBroker.GetCoreTagsByNameAsync(othersTag.Name);

                storedTags.Should().ContainSingle().Which.Id.Should().Be(othersTag.Id);

                IReadOnlyList<CoreAssociation> storedPairings =
                    await this.apiBroker.GetCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                storedPairings.Should().ContainSingle()
                    .Which.EntityBKeyId.Should().Be(othersTag.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(othersTag.Name);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldWithholdARejectedTagOverHttpAsAHiddenTagIsAnsweredAsync()
        {
            // given
            // No row holds the rejected tag's pair with the post, so only the withholding of a
            // rejected tag keeps the suggestion from pairing it.
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string otherUserId = this.apiBroker.ActAsContributor();
            CoreTag rejectedTag = await this.apiBroker.InsertRejectedTagAsync(otherUserId);
            this.apiBroker.ActAsContributor();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, rejectedTag.Name);

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                AssociationSubmissionResult actualResult =
                    await ReadSubmissionResultAsync(actualResponse);

                // then: the answer ShouldPairATagTheCallerCannotSeeOverHttpAsync gets
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSubmissionStatus.Created);

                IReadOnlyList<CoreTag> storedTags =
                    await this.apiBroker.GetCoreTagsByNameAsync(rejectedTag.Name);

                storedTags.Should().ContainSingle().Which.Id.Should().Be(rejectedTag.Id);

                IReadOnlyList<CoreAssociation> storedPairings =
                    await this.apiBroker.GetCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                storedPairings.Should().BeEmpty();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(rejectedTag.Name);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldAnswerCreatedOverHttpForATagTheCallerCannotSeeWhosePairIsHeldAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string otherUserId = this.apiBroker.ActAsContributor();
            CoreTag othersTag = await this.apiBroker.InsertDraftTagAsync(otherUserId);
            this.apiBroker.ActAsContributor();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, othersTag.Name);

            try
            {
                // The contributor's pairing of the post with the tag, at Submitted, as
                // ShouldPairATagTheCallerCannotSeeOverHttpAsync stores it.
                await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                AssociationSubmissionResult actualResult =
                    await ReadSubmissionResultAsync(actualResponse);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                actualResult.Status.Should().Be(AssociationSubmissionStatus.Created);

                IReadOnlyList<CoreAssociation> storedPairings =
                    await this.apiBroker.GetCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                CoreAssociation storedPairing = storedPairings.Should().ContainSingle().Subject;
                storedPairing.EntityBKeyId.Should().Be(othersTag.Id);
                storedPairing.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(othersTag.Name);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostTagSuggestionIfCallerIsAnonymousAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            string inputName = CreateUnheldTagName();
            TagSuggestion inputSuggestion = CreateTagSuggestion(publishedContentItem.Id, inputName);
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                string actualBody = await actualResponse.Content.ReadAsStringAsync();

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // The attribute's challenge answers with no body, where the orchestration's own
                // 401 carries one, so an empty body shows the attribute turned the caller away.
                actualBody.Should().BeEmpty();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreTagsByNameAsync(inputName);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPostTagSuggestionIfThePostDoesNotExistAsync()
        {
            // given
            Guid nonExistentContentItemId = Guid.NewGuid();
            string inputName = CreateUnheldTagName();
            TagSuggestion inputSuggestion = CreateTagSuggestion(nonExistentContentItemId, inputName);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagSuggestionAsync(inputSuggestion);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagsByNameAsync(inputName);
            }
        }

        private static TagSuggestion CreateTagSuggestion(Guid contentItemId, string name) =>
            new TagSuggestion
            {
                ContentItemId = contentItemId,
                Name = name
            };

        // A name no tag holds and no other test posts: thirty characters, the column's limit, of
        // a fresh Guid.
        private static string CreateUnheldTagName() =>
            Guid.NewGuid().ToString("N").Substring(0, 30);

        private static async ValueTask<AssociationSubmissionResult> ReadSubmissionResultAsync(
            HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<AssociationSubmissionResult>();
    }
}
