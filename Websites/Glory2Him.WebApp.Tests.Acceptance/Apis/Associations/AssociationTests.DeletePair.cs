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
        public async Task ShouldWithdrawAReactionOverHttpAsync()
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
                    await this.apiBroker.DeleteAssociationPairAsync(inputPair);

                string actualBody = await actualResponse.Content.ReadAsStringAsync();

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                actualBody.Should().BeEmpty();

                // The status alone cannot tell a withdrawal from a route that answered 204 and
                // did nothing; the reaction coming back as a revival of the withdrawn row can.
                HttpResponseMessage repostResponse =
                    await this.apiBroker.PostAssociationAsync(inputPair);

                AssociationSuggestionResult repostResult = await ReadResultAsync(repostResponse);
                repostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                repostResult.Status.Should().Be(AssociationSuggestionStatus.Restored);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldLeaveTheHeldReactionWhenAnotherIsWithdrawnOverHttpAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association heldPair = CreateReactionPair(publishedContentItem.Id, seededLoveReactionId);
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededJoyReactionId);
            this.apiBroker.ActAsContributor();

            try
            {
                await this.apiBroker.PostAssociationAsync(heldPair);

                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.DeleteAssociationPairAsync(inputPair);

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                // Love still live is what answers AlreadyApproved; had the withdrawal taken it,
                // posting it again would revive it and answer Restored.
                HttpResponseMessage repostResponse =
                    await this.apiBroker.PostAssociationAsync(heldPair);

                AssociationSuggestionResult repostResult = await ReadResultAsync(repostResponse);
                repostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                repostResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyApproved);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreAssociationsOnContentItemAsync(publishedContentItem.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeletePairIfCallerIsAnonymousAsync()
        {
            // given
            CoreContentItem publishedContentItem = await InsertPublishedContentItemAsync();
            Association inputPair = CreateReactionPair(publishedContentItem.Id, seededAmenReactionId);
            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.DeleteAssociationPairAsync(inputPair);

                string actualBody = await actualResponse.Content.ReadAsStringAsync();

                // then
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // The attribute's challenge answers with no body, where the orchestration's own
                // refusal would carry problem details: an empty body shows the attribute turned
                // the caller away before the orchestration was asked.
                actualBody.Should().BeEmpty();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnBadRequestOnDeletePairIfThePairIsEditorialAsync()
        {
            // given
            // A content item and a tag: neither endpoint is personal, so the withdrawal refuses
            // the pair before it reads anything, and no row needs arranging.
            var inputPair = new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid()
            };

            this.apiBroker.ActAsContributor();

            // when
            HttpResponseMessage actualResponse =
                await this.apiBroker.DeleteAssociationPairAsync(inputPair);

            // then
            actualResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
