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
        [Fact]
        public async Task ShouldGetUserClaimValueAsync()
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string type = GetRandomString();
            string value = GetRandomString();
            string expectedResult = value;

            this.userServiceMock.Setup(service =>
                service.GetUserClaimValueAsync(claimsPrincipal, type))
                    .ReturnsAsync(value);

            // When
            string actualResult = await this.userClient.GetUserClaimValueAsync(claimsPrincipal, type);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.userServiceMock.Verify(service =>
                service.GetUserClaimValueAsync(claimsPrincipal, type),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
