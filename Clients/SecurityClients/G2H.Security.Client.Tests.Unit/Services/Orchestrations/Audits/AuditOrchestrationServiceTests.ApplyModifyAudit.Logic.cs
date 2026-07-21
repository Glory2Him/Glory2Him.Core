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

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldApplyModifyAuditForDynamicObjectAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal();
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;
            string randomUserId = GetRandomString();
            var inputPerson = new Person { Name = GetRandomString() };
            var updatedPerson = new Person { Name = GetRandomString() };
            var expectedResult = updatedPerson;

            var securityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedWhen",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedWhen",
                UpdatedWhenPropertyType = typeof(DateTimeOffset)
            };

            this.userServiceMock.Setup(service =>
                service.GetUserIdAsync(inputClaimsPrincipal))
                    .ReturnsAsync(randomUserId);

            this.auditServiceMock.Setup(service =>
                service.ApplyModifyAuditValuesAsync(inputPerson, randomUserId, securityConfigurations))
                    .ReturnsAsync(updatedPerson);

            // When
            var actualResult = await this.auditOrchestrationService
                .ApplyModifyAuditValuesAsync(inputPerson, inputClaimsPrincipal, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(inputClaimsPrincipal),
                    Times.Once);

            this.auditServiceMock.Verify(service =>
                service.ApplyModifyAuditValuesAsync(inputPerson, randomUserId, securityConfigurations),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}