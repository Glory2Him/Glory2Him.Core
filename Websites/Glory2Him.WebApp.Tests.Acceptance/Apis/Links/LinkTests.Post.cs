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
using Glory2Him.WebApp.Tests.Acceptance.Models.Links;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Links
{
    public partial class LinkApiTests
    {
        [Fact]
        public async Task ShouldPostLinkAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;
            Link expectedLink = inputLink;

            try
            {
                // when
                Link createdLink =
                    await this.apiBroker.PostLinkAsync(inputLink);

                Link actualLink =
                    await this.apiBroker.GetLinkByIdAsync(createdLink.Id);

                // then
                actualLink.Should().BeEquivalentTo(expectedLink, options => options
                    .Excluding(property => property.Id)
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen)

                    // Derived, not echoed. GroupId and Version are assigned by
                    // the add because a new link is version 1 of its own group
                    // (12.4.2 business rule 6, 3.4.1). A link has no ContentHash:
                    // no duplicate-content rule means nothing to hash.
                    .Excluding(property => property.GroupId)
                    .Excluding(property => property.Version));

                // The derived fields are asserted as DERIVED rather than skipped: a service that
                // silently left them unset would otherwise pass the exclusions above.
                createdLink.GroupId.Should().NotBe(Guid.Empty);
                createdLink.Version.Should().Be(1);

                inputLink.Id = createdLink.Id;
            }
            finally
            {
                await this.apiBroker.RemoveCoreLinkByIdAsync(inputLink.Id);
            }
        }
    }
}
