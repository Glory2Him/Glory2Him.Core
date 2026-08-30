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
using G2H.Security.Client.Models.Foundations.Users;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldGetUserAsync()
        {
            // Given
            string userId = GetRandomString();
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId);

            User expectedUser = new User(
                userId: claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value!,
                userName: claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value!,
                givenName: claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value!,
                surname: claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value!,
                displayName: claimsPrincipal.FindFirst("displayName")?.Value!,
                email: claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value!,
                jobTitle: claimsPrincipal.FindFirst("jobTitle")?.Value!,
                roles: claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList(),
                claims: claimsPrincipal.Claims.ToList());

            // When
            User actualUser = await this.userService.GetUserAsync(claimsPrincipal);

            // Then
            actualUser.Should().BeEquivalentTo(expectedUser);
        }

        [Fact]
        public async Task ShouldGetUserNameFromTheNameClaimAndNotTheEmailAsync()
        {
            // Given: an account whose username and email are properly distinct. UserName is read
            // from ClaimTypes.Name so that callers naming the actor never reach for Email — an
            // email put on an event envelope's security context is signed into every stored
            // event that caller causes, and cannot be scrubbed afterwards.
            string userName = GetRandomString();
            string email = $"{GetRandomString()}@example.org";

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, GetRandomString()),
                        new Claim(ClaimTypes.Name, userName),
                        new Claim(ClaimTypes.Email, email)
                    },
                    authenticationType: "Test"));

            // When
            User actualUser = await this.userService.GetUserAsync(claimsPrincipal);

            // Then
            actualUser.UserName.Should().Be(userName);
            actualUser.UserName.Should().NotBe(email);
            actualUser.Email.Should().Be(email);
        }
    }
}
