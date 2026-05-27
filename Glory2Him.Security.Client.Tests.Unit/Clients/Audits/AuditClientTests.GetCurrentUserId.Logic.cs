// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
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
