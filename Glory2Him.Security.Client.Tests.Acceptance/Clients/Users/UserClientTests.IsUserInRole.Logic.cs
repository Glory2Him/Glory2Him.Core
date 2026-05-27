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
        public async Task ShouldPerformIsUserInRoleAsync()
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string roleName = claimsPrincipal.FindAll(ClaimTypes.Role).FirstOrDefault().Value;
            bool expectedResult = true;

            // When
            bool actualResult = await this.securityClient.Users.IsUserInRoleAsync(claimsPrincipal, roleName);

            // Then
            actualResult.Should().Be(expectedResult);
        }
    }
}
