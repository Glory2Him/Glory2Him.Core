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
        public async Task ShouldGetUserIdAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            string randomUserId = GetRandomString();

            this.userServiceMock.Setup(service =>
                service.GetUserIdAsync(randomClaimsPrincipal))
                    .ReturnsAsync(randomUserId);

            string expectedUserId = randomUserId;

            // When
            string actualUserId = await this.userClient.GetUserIdAsync(randomClaimsPrincipal);

            // Then
            actualUserId.Should().BeEquivalentTo(expectedUserId);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(randomClaimsPrincipal),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
