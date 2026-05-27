// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldGetUserClaimValuesAsync()
        {
            // Given
            string userId = GetRandomString();
            string type = ClaimTypes.GivenName;
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal(userId);

            IReadOnlyList<string> givenNames = claimsPrincipal.FindAll(type)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();

            IReadOnlyList<string> expectedResult = givenNames;

            // When
            IReadOnlyList<string> actualResult =
                await this.userService.GetUserClaimValuesAsync(claimsPrincipal, type);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);
        }
    }
}
