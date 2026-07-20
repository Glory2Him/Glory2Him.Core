// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPerformUserHasClaimTypeAsync(bool hasClaimType)
        {
            // Given
            string claimType = GetRandomString();
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            bool expectedResult = hasClaimType;

            this.userServiceMock.Setup(service =>
                service.UserHasClaimAsync(claimsPrincipal, claimType))
                    .ReturnsAsync(expectedResult);

            // When
            bool actualResult = await this.userClient.UserHasClaimAsync(claimsPrincipal, claimType);

            // Then
            actualResult.Should().Be(expectedResult);

            this.userServiceMock.Verify(service =>
                service.UserHasClaimAsync(claimsPrincipal, claimType),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPerformUserHasClaimTypeWithValueAsync(bool hasClaimType)
        {
            // Given
            ClaimsPrincipal claimsPrincipal = CreateRandomClaimsPrincipal();
            string claimType = GetRandomString();
            string claimValue = GetRandomString();
            bool expectedResult = hasClaimType;

            this.userServiceMock.Setup(service =>
                service.UserHasClaimAsync(claimsPrincipal, claimType, claimValue))
                    .ReturnsAsync(expectedResult);

            // When
            bool actualResult = await this.userClient.UserHasClaimAsync(claimsPrincipal, claimType, claimValue);

            // Then
            actualResult.Should().Be(expectedResult);

            this.userServiceMock.Verify(service =>
                service.UserHasClaimAsync(claimsPrincipal, claimType, claimValue),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}
