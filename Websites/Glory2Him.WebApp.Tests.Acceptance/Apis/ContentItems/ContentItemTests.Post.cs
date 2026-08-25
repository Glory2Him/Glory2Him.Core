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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    public partial class ContentItemApiTests
    {
        [Fact]
        public async Task ShouldPostContentItemAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            ContentItem expectedContentItem = inputContentItem;

            try
            {
                // when
                ContentItem createdContentItem =
                    await this.apiBroker.PostContentItemAsync(inputContentItem);

                ContentItem actualContentItem =
                    await this.apiBroker.GetContentItemByIdAsync(createdContentItem.Id);

                // then
                actualContentItem.Should().BeEquivalentTo(expectedContentItem, options => options
                    .Excluding(property => property.Id)
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen)

                    // Derived, not echoed. ContentHash is computed from Content (§3.4.2), and
                    // GroupId and Version are assigned by the add because a new item is version
                    // 1 of its own group (§12.4.1 rule 6, §3.4.1). Asserting them against the
                    // request would be asserting that the service ignored its own rules.
                    .Excluding(property => property.ContentHash)
                    .Excluding(property => property.GroupId)
                    .Excluding(property => property.Version));

                // The derived fields are asserted as DERIVED rather than skipped: a service that
                // silently left them unset would otherwise pass the exclusions above.
                createdContentItem.ContentHash.Should().NotBeNullOrWhiteSpace();
                createdContentItem.GroupId.Should().NotBe(Guid.Empty);
                createdContentItem.Version.Should().Be(1);

                inputContentItem.Id = createdContentItem.Id;
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(inputContentItem.Id);
            }
        }
    }
}
