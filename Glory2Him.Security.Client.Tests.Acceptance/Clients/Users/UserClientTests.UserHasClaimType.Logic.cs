// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace Glory2Him.Security.Client.Tests.Unit.Clients.Users
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
