// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users;

namespace Glory2Him.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Fact]
        public async Task ShouldGetUserAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            User userFromClaimsPrincipal = GetUser(randomClaimsPrincipal);
            User expectedUser = userFromClaimsPrincipal;

            // When
            User actualUser = await this.securityClient.Users.GetUserAsync(randomClaimsPrincipal);

            // Then
            actualUser.Should().BeEquivalentTo(expectedUser);
        }
    }
}
