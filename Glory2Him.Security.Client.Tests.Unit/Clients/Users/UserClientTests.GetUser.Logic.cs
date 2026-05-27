// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users;
using Moq;

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

            this.userServiceMock.Setup(service =>
                service.GetUserAsync(randomClaimsPrincipal))
                    .ReturnsAsync(userFromClaimsPrincipal);

            User expectedUser = userFromClaimsPrincipal;

            // When
            User actualUser = await this.userClient.GetUserAsync(randomClaimsPrincipal);

            // Then
            actualUser.Should().BeEquivalentTo(expectedUser);

            this.userServiceMock.Verify(service =>
                service.GetUserAsync(randomClaimsPrincipal),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
