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

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Fact]
        public async Task ShouldPerformUserHasClaimTypeAsync()
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string claimType = claimsPrincipal.Claims.First().Type;
            bool expectedResult = true;

            // When
            bool actualResult = await this.securityClient.Users.UserHasClaimAsync(claimsPrincipal, claimType);

            // Then
            actualResult.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ShouldPerformUserHasClaimTypeAndValueAsync()
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string claimType = claimsPrincipal.Claims.First().Type;
            string claimValue = claimsPrincipal.Claims.First().Value;
            bool expectedResult = true;

            // When
            bool actualResult = await this.securityClient.Users
                .UserHasClaimAsync(claimsPrincipal, claimType, claimValue);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
