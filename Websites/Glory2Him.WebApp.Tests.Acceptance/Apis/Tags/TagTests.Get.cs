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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    public partial class TagApiTests
    {
        [Fact]
        public async Task ShouldGetAllTagsAsync()
        {
            // given
            List<Tag> randomTags = await PostRandomTagsAsync();
            List<Tag> expectedTags = randomTags;

            // when
            List<Tag> actualTags = await this.apiBroker.GetAllTagsAsync();

            // then
            try
            {
                foreach (Tag expectedTag in expectedTags)
                {
                    Tag actualTag =
                        actualTags.Single(tag => tag.Id == expectedTag.Id);

                    actualTag.Should().BeEquivalentTo(expectedTag, options => options
                        .Excluding(property => property.CreatedBy)
                        .Excluding(property => property.CreatedWhen)
                        .Excluding(property => property.UpdatedBy)
                        .Excluding(property => property.UpdatedWhen));
                }
            }
            finally
            {
                // Cleanup is driven off what was POSTED, not off what the read returned, and
                // runs even when an assertion throws. Deleting inside the assertion loop left
                // every row the loop had not reached yet live forever, and this suite runs
                // against a persistent database that nothing else resets.
                foreach (Tag postedTag in randomTags)
                {
                    await this.apiBroker.DeleteTagByIdAsync(postedTag.Id);
                }
            }
        }
    }
}
