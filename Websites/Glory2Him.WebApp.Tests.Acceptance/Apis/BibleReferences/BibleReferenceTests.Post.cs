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
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.BibleReferences
{
    public partial class BibleReferenceApiTests
    {
        [Fact]
        public async Task ShouldPostBibleReferenceAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference expectedBibleReference = inputBibleReference;

            try
            {
                // when
                await this.apiBroker.PostBibleReferenceAsync(inputBibleReference);

                BibleReference actualBibleReference =
                    await this.apiBroker.GetBibleReferenceByIdAsync(inputBibleReference.Id);

                // then
                actualBibleReference.Should().BeEquivalentTo(expectedBibleReference, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(inputBibleReference.Id);
            }
        }
    }
}
