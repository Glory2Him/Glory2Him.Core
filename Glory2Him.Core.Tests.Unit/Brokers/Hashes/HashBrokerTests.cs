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
using Glory2Him.Core.Brokers.Hashes;

namespace Glory2Him.Core.Tests.Unit.Brokers.Hashes
{
    public class HashBrokerTests
    {
        // Pins the hashing half of the FROZEN content hash contract (design §3.4.2):
        // SHA-256 over UTF-8 bytes rendered as lowercase hex (64 chars). The known
        // vector below is the SHA-256 of "hello world".
        [Fact]
        public async Task ShouldComputeSha256HashAsLowercaseHexAsync()
        {
            // given
            var hashBroker = new HashBroker();
            string inputText = "hello world";

            string expectedHash =
                "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";

            // when
            string actualHash = await hashBroker.ComputeSha256HashAsync(inputText);

            // then
            actualHash.Should().Be(expectedHash);
        }
    }
}
