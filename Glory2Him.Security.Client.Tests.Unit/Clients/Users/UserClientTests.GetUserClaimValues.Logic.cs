// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Fact]
        public async Task ShouldGetUserClaimValuesAsync()
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string type = GetRandomString();

            IReadOnlyList<string> randomValues = Enumerable.Range(0, 5) // 5 items
                .Select(_ => GetRandomString())
                .ToArray();

            IReadOnlyList<string> expectedResult = randomValues;

            this.userServiceMock.Setup(service =>
                service.GetUserClaimValuesAsync(claimsPrincipal, type))
                    .ReturnsAsync(randomValues);

            // When
            IReadOnlyList<string> actualResult =
                await this.userClient.GetUserClaimValuesAsync(claimsPrincipal, type);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.userServiceMock.Verify(service =>
                service.GetUserClaimValuesAsync(claimsPrincipal, type),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
