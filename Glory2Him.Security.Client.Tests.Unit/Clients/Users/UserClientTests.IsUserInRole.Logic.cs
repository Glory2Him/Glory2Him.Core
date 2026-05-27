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
        public async Task ShouldPerformIsUserInRoleAsync(bool isInRole)
        {
            // Given
            string roleName = GetRandomString();
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            bool expectedResult = isInRole;

            this.userServiceMock.Setup(service =>
                service.IsUserInRoleAsync(claimsPrincipal, roleName))
                    .ReturnsAsync(isInRole);

            // When
            bool actualResult = await this.userClient.IsUserInRoleAsync(claimsPrincipal, roleName);

            // Then
            actualResult.Should().Be(expectedResult);

            this.userServiceMock.Verify(service =>
                service.IsUserInRoleAsync(claimsPrincipal, roleName),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
