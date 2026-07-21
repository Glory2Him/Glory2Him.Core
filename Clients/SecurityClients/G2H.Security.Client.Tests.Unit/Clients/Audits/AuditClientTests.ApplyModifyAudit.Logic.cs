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
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;

namespace G2H.Security.Client.Tests.Clients.Audits
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