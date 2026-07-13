// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Users;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Theory]
        [InlineData("Users", true)]
        [InlineData("SuperUsers", false)]
        public async Task ShouldPerformIsUserInRoleAsync(string roleName, bool isInRole)
        {
            // Given
            string userId = GetRandomString();
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId);
            bool expectedResult = isInRole;

            User expectedUser = new User(
                userId: claimsPrincipal.FindFirst("oid")?.Value!,
                givenName: claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value!,
                surname: claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value!,
                displayName: claimsPrincipal.FindFirst("displayName")?.Value!,
                email: claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value!,
                jobTitle: claimsPrincipal.FindFirst("jobTitle")?.Value!,
                roles: claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList(),
                claims: claimsPrincipal.Claims.ToList());

            // When
            bool actualResult = await this.userService.IsUserInRoleAsync(claimsPrincipal, roleName);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
