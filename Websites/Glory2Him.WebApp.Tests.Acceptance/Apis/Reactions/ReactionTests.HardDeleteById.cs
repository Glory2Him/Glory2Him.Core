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
using Glory2Him.WebApp.Tests.Acceptance.Models.Reactions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Reactions
{
    public partial class ReactionApiTests
    {
        [Fact]
        public async Task ShouldHardDeleteReactionByIdAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            Reaction inputReaction = randomReaction;
            Reaction expectedReaction = inputReaction;

            try
            {
                // when
                Reaction deletedReaction =
                    await this.apiBroker.HardDeleteReactionByIdAsync(inputReaction.Id);

                List<Reaction> actualResult =
                    await this.apiBroker.GetSpecificReactionByIdAsync(inputReaction.Id);

                // then
                actualResult.Count().Should().Be(0);
            }
            finally
            {
                await this.apiBroker.RemoveCoreReactionByIdAsync(inputReaction.Id);
            }
        }
    }
}
