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

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
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
