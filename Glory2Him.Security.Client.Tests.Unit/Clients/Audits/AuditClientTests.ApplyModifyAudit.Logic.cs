// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Tests.Unit.Models;
using Moq;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Fact]
        public async Task ShouldApplyModifyAuditForDynamicObjectAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;
            var inputPerson = new Person { Name = GetRandomString() };
            var updatedPerson = new Person { Name = GetRandomString() };
            var expectedResult = updatedPerson;
            var securityConfigurations = new SecurityConfigurations();

            this.auditOrchestrationServiceMock.Setup(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ReturnsAsync(updatedPerson);

            // When
            var actualResult = await this.auditClient
                .ApplyModifyAuditValuesAsync(inputPerson, inputClaimsPrincipal, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}