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
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    public partial class TagApiTests
    {
        [Fact]
        public async Task ShouldPostTagAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            Tag inputTag = randomTag;
            Tag expectedTag = inputTag;
            Tag postedTag = null;

            try
            {
                // when
                postedTag = await this.apiBroker.PostTagAsync(inputTag);

                // the row is read back under the Id the post answered, never the one it was
                // sent: the tag's Id is the service's to mint (§1 rule 5)
                Tag actualTag =
                    await this.apiBroker.GetTagByIdAsync(postedTag.Id);

                // then
                actualTag.Should().BeEquivalentTo(expectedTag, options => options
                    .Excluding(property => property.Id)
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                if (postedTag is not null)
                {
                    await this.apiBroker.RemoveCoreTagByIdAsync(postedTag.Id);
                }
            }
        }

        [Fact]
        public async Task ShouldPostTagUnderAMintedIdAsync()
        {
            // given
            this.apiBroker.ActAsContributor();
            Tag randomTag = CreateRandomTag();
            Tag inputTag = randomTag;
            Guid postedId = inputTag.Id;
            Tag answeredTag = null;

            try
            {
                // when
                HttpResponseMessage actualResponse =
                    await this.apiBroker.PostTagForResponseAsync(inputTag);

                answeredTag = await actualResponse.Content.ReadFromJsonAsync<Tag>();

                Tag actualTag =
                    await this.apiBroker.GetTagByIdAsync(answeredTag.Id);

                // then: the route answers the Id the service minted, never the one it was sent
                // (TagService.TagSuggestions.md §1 rule 5), and the tag is read back under it
                actualResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                answeredTag.Id.Should().NotBeEmpty();
                answeredTag.Id.Should().NotBe(postedId);
                actualTag.Id.Should().Be(answeredTag.Id);
                actualTag.Name.Should().Be(inputTag.Name);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                if (answeredTag is not null)
                {
                    await this.apiBroker.RemoveCoreTagByIdAsync(answeredTag.Id);
                }
            }
        }
    }
}
