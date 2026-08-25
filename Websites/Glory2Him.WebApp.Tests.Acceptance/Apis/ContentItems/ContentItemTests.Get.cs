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
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    public partial class ContentItemApiTests
    {
        [Fact]
        public async Task ShouldGetAllContentItemsAsync()
        {
            // given
            List<ContentItem> randomContentItems = await PostRandomContentItemsAsync();
            List<ContentItem> expectedContentItems = randomContentItems;

            // when
            List<ContentItem> actualContentItems = await this.apiBroker.GetAllContentItemsAsync();

            // then
            try
            {
                foreach (ContentItem expectedContentItem in expectedContentItems)
                {
                    ContentItem actualContentItem =
                        actualContentItems.Single(contentItem => contentItem.Id == expectedContentItem.Id);

                    actualContentItem.Should().BeEquivalentTo(expectedContentItem, options => options
                        .Excluding(property => property.CreatedBy)
                        .Excluding(property => property.CreatedWhen)
                        .Excluding(property => property.UpdatedBy)
                        .Excluding(property => property.UpdatedWhen));
                }
            }
            finally
            {
                // Cleanup is driven off what was POSTED, not off what the read returned, runs
                // even when an assertion throws, and removes the row rather than soft-deleting
                // it. Deleting inside the assertion loop left every row the loop had not reached
                // yet, and going through the API left a soft-deleted row behind either way.
                foreach (ContentItem postedContentItem in randomContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(postedContentItem.Id);
                }
            }
        }
    }
}
