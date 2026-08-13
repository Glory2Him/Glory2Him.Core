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
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    public partial class TagApiTests
    {
        [Fact]
        public async Task ShouldSubmitTagByIdAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();

            // when
            await this.apiBroker.SubmitTagByIdAsync(randomTag.Id);

            Tag actualTag = await this.apiBroker
                .GetTagByIdAsync(randomTag.Id);

            // then
            actualTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            await this.apiBroker.DeleteTagByIdAsync(actualTag.Id);
        }
    }
}
