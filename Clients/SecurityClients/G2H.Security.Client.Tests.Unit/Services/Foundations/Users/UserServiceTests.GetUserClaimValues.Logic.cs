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
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldGetUserClaimValuesAsync()
        {
            // Given
            string userId = GetRandomString();
            string type = ClaimTypes.GivenName;
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId);

            IReadOnlyList<string> givenNames = claimsPrincipal.FindAll(type)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();

            IReadOnlyList<string> expectedResult = givenNames;

            // When
            IReadOnlyList<string> actualResult =
                await this.userService.GetUserClaimValuesAsync(claimsPrincipal, type);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);
        }
    }
}
