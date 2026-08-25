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
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.BibleReferences
{
    public partial class BibleReferenceApiTests
    {
        [Fact]
        public async Task ShouldDeleteBibleReferenceByIdAsync()
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference expectedBibleReference = inputBibleReference;

            try
            {
                // when
                BibleReference deletedBibleReference =
                    await this.apiBroker.DeleteBibleReferenceByIdAsync(inputBibleReference.Id);

                List<BibleReference> actualResult =
                    await this.apiBroker.GetSpecificBibleReferenceByIdAsync(inputBibleReference.Id);

                // then
                actualResult.Count().Should().Be(0);
            }
            finally
            {
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(inputBibleReference.Id);
            }
        }
    }
}
