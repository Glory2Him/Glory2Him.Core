// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Theory]
        [InlineData("username", true)]
        [InlineData("username", false)]
        [InlineData("", false)]
        public async Task ShouldGetCurrentUserIdAsync(string userId, bool isAuthenticated)
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal(isAuthenticated, userId);
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;

            string securityUserId = isAuthenticated
                ? userId
                : string.IsNullOrEmpty(userId)
                    ? "anonymous" : userId;

            var expectedResult = securityUserId;

            // When
            var actualResult = await this.securityClient.Audits.GetUserIdAsync(inputClaimsPrincipal);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);
        }
    }
}