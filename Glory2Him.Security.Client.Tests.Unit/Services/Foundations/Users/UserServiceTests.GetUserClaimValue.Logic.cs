// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldGetUserClaimValueAsync()
        {
            // Given
            string userId = GetRandomString();
            string type = ClaimTypes.GivenName;
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId);
            Claim givenName = claimsPrincipal.FindFirst(type);
            string expectedResult = givenName?.Value;

            // When
            string actualResult = await this.userService.GetUserClaimValueAsync(claimsPrincipal, type);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
