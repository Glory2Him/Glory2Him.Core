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
        [Theory]
        [InlineData("username", true)]
        [InlineData("username", false)]
        [InlineData("", false)]
        public async Task ShouldGetUserIdAsync(string userId, bool isAuthenticated)
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal(userId, isAuthenticated);
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;

            string securityUserId = isAuthenticated
                ? userId
                : string.IsNullOrEmpty(userId)
                    ? "anonymous" : userId;

            string expectedUserId = securityUserId;

            // When
            string actualUserId = await this.userService.GetUserIdAsync(randomClaimsPrincipal);

            // Then
            actualUserId.Should().BeEquivalentTo(expectedUserId);
        }
    }
}
