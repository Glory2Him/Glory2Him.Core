// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace Glory2Him.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPerformIsUserAuthenticatedAsync(bool isAuthenticated)
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(isAuthenticated);
            bool expectedResult = isAuthenticated;

            // When
            bool actualResult = await this.securityClient.Users.IsUserAuthenticatedAsync(claimsPrincipal);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
