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
    }
}
