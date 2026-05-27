// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

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

            this.userServiceMock.Setup(service =>
                service.IsUserAuthenticatedAsync(claimsPrincipal))
                    .ReturnsAsync(expectedResult);

            // When
            bool actualResult = await this.userClient.IsUserAuthenticatedAsync(claimsPrincipal);

            // Then
            actualResult.Should().Be(expectedResult);

            this.userServiceMock.Verify(service =>
                service.IsUserAuthenticatedAsync(claimsPrincipal),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
