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

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Services.Foundations.Users;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    /// <summary>
    /// Which claim the user id is read from. The value resolved here becomes CreatedBy on every
    /// audited entity and is what an ownership check compares against, so the default matters as
    /// much as the override does.
    /// </summary>
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldReadUserIdFromNameIdentifierByDefaultAsync()
        {
            // given: ASP.NET Core Identity puts the account's primary key here, so a host on
            // Identity needs no configuration at all
            string userId = GetRandomString();

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    authenticationType: "Test"));

            var userService = new UserService();

            // when
            string actualUserId = await userService.GetUserIdAsync(claimsPrincipal);

            // then
            actualUserId.Should().Be(userId);
        }

        [Fact]
        public async Task ShouldReadUserIdFromAConfiguredClaimTypeAsync()
        {
            // given: a host on another provider — Entra carries the object id in oid
            string objectId = GetRandomString();

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("oid", objectId) },
                    authenticationType: "Test"));

            var userService = new UserService(new UserIdentityConfigurations
            {
                UserIdClaimTypes = new[] { "oid" }
            });

            // when
            string actualUserId = await userService.GetUserIdAsync(claimsPrincipal);

            // then
            actualUserId.Should().Be(objectId);
        }

        [Fact]
        public async Task ShouldTakeTheFirstConfiguredClaimPresentAsync()
        {
            // given: a host federating two providers lists both and lets precedence decide
            string objectId = GetRandomString();
            string nameIdentifier = GetRandomString();

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, nameIdentifier),
                        new Claim("oid", objectId)
                    },
                    authenticationType: "Test"));

            var userService = new UserService(new UserIdentityConfigurations
            {
                UserIdClaimTypes = new[] { "oid", ClaimTypes.NameIdentifier }
            });

            // when
            string actualUserId = await userService.GetUserIdAsync(claimsPrincipal);

            // then: oid is listed first, so it wins even though both are present
            actualUserId.Should().Be(objectId);
        }

        [Fact]
        public async Task ShouldFallThroughToTheNextConfiguredClaimWhenTheFirstIsAbsentAsync()
        {
            // given
            string nameIdentifier = GetRandomString();

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, nameIdentifier) },
                    authenticationType: "Test"));

            var userService = new UserService(new UserIdentityConfigurations
            {
                UserIdClaimTypes = new[] { "oid", ClaimTypes.NameIdentifier }
            });

            // when
            string actualUserId = await userService.GetUserIdAsync(claimsPrincipal);

            // then
            actualUserId.Should().Be(nameIdentifier);
        }

        [Fact]
        public async Task ShouldNotResolveAUserIdFromAUsernameClaimAsync()
        {
            // given: ClaimTypes.Name is a USERNAME. It was a fallback before this was
            // configurable, and it must not be one: two accounts can share a display name, so an
            // ownership check matching on it is a privilege escalation.
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, GetRandomString()) },
                    authenticationType: "Test"));

            var userService = new UserService();

            // when
            string actualUserId = await userService.GetUserIdAsync(claimsPrincipal);

            // then: no configured claim is present, so nothing is resolved
            actualUserId.Should().BeNullOrEmpty();
        }
    }
}
