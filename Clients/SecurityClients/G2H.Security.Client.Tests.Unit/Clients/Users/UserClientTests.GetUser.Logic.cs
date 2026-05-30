// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Users;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
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
