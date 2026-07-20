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
using G2H.Security.Client.Models.Clients.Audits.Exceptions;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;
using Xeptions;

namespace G2H.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowDependencyValidationOnApplyModifyAuditIfDependencyValidationOccursAndLogItAsync(
            Xeption validationException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedAuditClientValidationException =
                new AuditClientValidationException(
                    message: "Audit client validation error occurred, fix the error and try again.",
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(validationException);

            // when
            ValueTask<Person> task = this.auditClient.ApplyModifyAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditClientValidationException actualAuditClientValidationException =
                await Assert.ThrowsAsync<AuditClientValidationException>(task.AsTask);

            // then
            actualAuditClientValidationException.Should()
                .BeEquivalentTo(expectedAuditClientValidationException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnApplyModifyAuditIfDependencyExceptionOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedAuditClientDependencyException =
                new AuditClientDependencyException(
                    message: "Audit client dependency error occurred, please contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<Person> task = this.auditClient.ApplyModifyAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditClientDependencyException actualAuditClientDependencyException =
                await Assert.ThrowsAsync<AuditClientDependencyException>(task.AsTask);

            // then
            actualAuditClientDependencyException.Should().BeEquivalentTo(expectedAuditClientDependencyException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.ApplyModifyAuditValuesAsync(
                    It.IsAny<Person>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnApplyModifyAuditIfServiceErrorOccursAndLogItAsync()
        {
            //Given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();
            var serviceException = new Exception();

            var expectedAuditClientServiceException =
                new AuditClientServiceException(
                    message: "Audit client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.auditOrchestrationServiceMock.Setup(service =>
               service.ApplyModifyAuditValuesAsync(
                   It.IsAny<Person>(),
                   It.IsAny<ClaimsPrincipal>(),
                   It.IsAny<SecurityConfigurations>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Person> task = this.auditClient.ApplyModifyAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditClientServiceException actualAuditClientServiceException =
                await Assert.ThrowsAsync<AuditClientServiceException>(task.AsTask);

            // then
            actualAuditClientServiceException.Should().BeEquivalentTo(expectedAuditClientServiceException);

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
