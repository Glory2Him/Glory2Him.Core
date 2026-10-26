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
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    public partial class AssociationApiTests
    {
        [Fact]
        public async Task ShouldServeAPublicEditorialAssociationByIdToAnAnonymousCallerAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            CoreTag publishedTag = null;

            try
            {
                publishedTag = await this.apiBroker.InsertPublishedTagAsync(
                    authorUserId: "acceptance-association-reads");

                CoreAssociation insertedAssociation =
                    await this.apiBroker.InsertPublishedEditorialTagAssociationAsync(
                        publishedContentItem,
                        publishedTag,
                        authorUserId: "acceptance-association-reads");

                CoreAssociation expectedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(insertedAssociation.Id);

                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationByIdAsync(insertedAssociation.Id);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                CoreAssociation actualAssociation =
                    await actualResponse.Content.ReadFromJsonAsync<CoreAssociation>();

                actualAssociation.Should().BeEquivalentTo(expectedAssociation);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                if (publishedTag is not null)
                {
                    await this.apiBroker.RemoveCoreTagByIdAsync(publishedTag.Id);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldServeAReaderTheirOwnReactionByIdAsync()
        {
            // given
            CoreContentItem reactedContentItem = await InsertPublishedContentItemAsync();

            try
            {
                (string readerId, Guid reactionId) =
                    await GiveAmenAsANewReaderAsync(reactedContentItem.Id);

                CoreAssociation expectedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(reactionId);

                this.apiBroker.ActAs(readerId);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationByIdAsync(reactionId);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                CoreAssociation actualAssociation =
                    await actualResponse.Content.ReadFromJsonAsync<CoreAssociation>();

                actualAssociation.Should().BeEquivalentTo(expectedAssociation);
                actualAssociation.UserId.Should().Be(readerId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(reactedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(reactedContentItem.Id);
            }
        }
    }
}
