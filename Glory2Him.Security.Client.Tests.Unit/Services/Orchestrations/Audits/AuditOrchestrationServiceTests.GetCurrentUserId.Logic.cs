// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldGetCurrentUserIdAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;
            string randomUserId = GetRandomString();
            var expectedResult = randomUserId;

            this.userServiceMock.Setup(service =>
                service.GetUserIdAsync(inputClaimsPrincipal))
                    .ReturnsAsync(randomUserId);

            // When
            var actualResult = await this.auditOrchestrationService
                .GetCurrentUserIdAsync(inputClaimsPrincipal);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(inputClaimsPrincipal),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}