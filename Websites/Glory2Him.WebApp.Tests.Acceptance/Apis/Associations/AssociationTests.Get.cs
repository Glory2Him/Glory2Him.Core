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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Securities;
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Associations
{
    public partial class AssociationApiTests
    {
        [Fact]
        public async Task ShouldServeAPublicEditorialAssociationToAnAnonymousCallerAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            CoreContentItem otherPublishedContentItem = await InsertPublishedContentItemAsync();
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

                // The same tag on another post, as public as the first, so an answer the filter
                // did not narrow holds two rows rather than one.
                await this.apiBroker.InsertPublishedEditorialTagAssociationAsync(
                    otherPublishedContentItem,
                    publishedTag,
                    authorUserId: "acceptance-association-reads");

                CoreAssociation expectedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(insertedAssociation.Id);

                string contentItemAssociationsQuery =
                    CreateContentItemAssociationsQuery(expectedAssociation);

                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationsAsync(contentItemAssociationsQuery);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<CoreAssociation> actualAssociations =
                    await ReadAssociationsAsync(actualResponse);

                actualAssociations.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(expectedAssociation);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);

                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(
                    otherPublishedContentItem.Id);

                if (publishedTag is not null)
                {
                    await this.apiBroker.RemoveCoreTagByIdAsync(publishedTag.Id);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(otherPublishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldServeAReaderTheirOwnReactionAsync()
        {
            // given
            CoreContentItem reactedContentItem = await InsertPublishedContentItemAsync();

            try
            {
                (string readerId, Guid reactionId) =
                    await GiveAmenAsANewReaderAsync(reactedContentItem.Id);

                CoreAssociation expectedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(reactionId);

                string contentItemAssociationsQuery =
                    CreateContentItemAssociationsQuery(expectedAssociation);

                this.apiBroker.ActAs(readerId);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationsAsync(contentItemAssociationsQuery);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<CoreAssociation> actualAssociations =
                    await ReadAssociationsAsync(actualResponse);

                CoreAssociation actualAssociation =
                    actualAssociations.Should().ContainSingle().Subject;

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

        [Fact]
        public async Task ShouldServeAReadersReactionToTheReviewTierAsync()
        {
            // given
            CoreContentItem reactedContentItem = await InsertPublishedContentItemAsync();

            try
            {
                (string readerId, Guid reactionId) =
                    await GiveAmenAsANewReaderAsync(reactedContentItem.Id);

                CoreAssociation expectedAssociation =
                    await this.apiBroker.GetCoreAssociationByIdAsync(reactionId);

                string contentItemAssociationsQuery =
                    CreateContentItemAssociationsQuery(expectedAssociation);

                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationsAsync(contentItemAssociationsQuery);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<CoreAssociation> actualAssociations =
                    await ReadAssociationsAsync(actualResponse);

                CoreAssociation actualAssociation =
                    actualAssociations.Should().ContainSingle().Subject;

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

        /// <summary>
        /// The post also carries a public editorial tag, which the anonymous caller is answered,
        /// so an answer without the reaction is the posture withholding it rather than a query
        /// that found nothing.
        /// </summary>
        [Fact]
        public async Task ShouldNotServeAReadersReactionToAnAnonymousCallerAsync()
        {
            // given
            CoreContentItem reactedContentItem = await InsertPublishedContentItemAsync();
            CoreTag publishedTag = null;

            try
            {
                publishedTag = await this.apiBroker.InsertPublishedTagAsync(
                    authorUserId: "acceptance-association-reads");

                CoreAssociation editorialAssociation =
                    await this.apiBroker.InsertPublishedEditorialTagAssociationAsync(
                        reactedContentItem,
                        publishedTag,
                        authorUserId: "acceptance-association-reads");

                (_, Guid reactionId) = await GiveAmenAsANewReaderAsync(reactedContentItem.Id);

                CoreAssociation storedReaction =
                    await this.apiBroker.GetCoreAssociationByIdAsync(reactionId);

                string contentItemAssociationsQuery =
                    CreateContentItemAssociationsQuery(storedReaction);

                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.GetAssociationsAsync(contentItemAssociationsQuery);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                List<CoreAssociation> actualAssociations =
                    await ReadAssociationsAsync(actualResponse);

                actualAssociations.Should().ContainSingle(association =>
                    association.Id == editorialAssociation.Id);

                actualAssociations.Should().NotContain(association =>
                    association.Id == reactionId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(reactedContentItem.Id);

                if (publishedTag is not null)
                {
                    await this.apiBroker.RemoveCoreTagByIdAsync(publishedTag.Id);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(reactedContentItem.Id);
            }
        }
    }
}
