// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPerformIsUserAuthenticatedAsync(bool isAuthenticated)
        {
            // Given
            string userId = GetRandomString();
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId, isAuthenticated);
            bool expectedResult = isAuthenticated;

            User expectedUser = new User(
                userId: claimsPrincipal.FindFirst("oid")?.Value,
                givenName: claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value,
                surname: claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value,
                displayName: claimsPrincipal.FindFirst("displayName")?.Value,
                email: claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value,
                jobTitle: claimsPrincipal.FindFirst("jobTitle")?.Value,
                roles: claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList(),
                claims: claimsPrincipal.Claims.ToList());

            // When
            bool actualResult = await this.userService.IsUserAuthenticatedAsync(claimsPrincipal);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
