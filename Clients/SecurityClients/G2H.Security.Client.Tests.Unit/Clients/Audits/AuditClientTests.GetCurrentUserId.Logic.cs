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

namespace G2H.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Fact]
        public async Task ShouldGetUserIdAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            string randomUserId = GetRandomString();

            this.auditOrchestrationServiceMock.Setup(service =>
                service.GetCurrentUserIdAsync(randomClaimsPrincipal))
                    .ReturnsAsync(randomUserId);

            string expectedUserId = randomUserId;

            // When
            string actualUserId = await this.auditClient.GetUserIdAsync(randomClaimsPrincipal);

            // Then
            actualUserId.Should().BeEquivalentTo(expectedUserId);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.GetCurrentUserIdAsync(randomClaimsPrincipal),
                    Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
