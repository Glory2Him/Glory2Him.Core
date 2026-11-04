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
