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
using G2H.Security.Client.Models.Orchestrations.Audits.Exceptions;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;
using Xeptions;

namespace G2H.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationOnApplyAddAuditIfDependencyValidationOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyValidationException(
                    message: "Audit orchestration dependency validation error occurred, fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            this.userServiceMock.Setup(service =>
               service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                   .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService.ApplyAddAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationDependencyValidationException>(task.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedDependencyException);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnApplyAddAuditIfDependencyExceptionOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyException(
                    message: "Audit orchestration dependency error occurred, fix the errors and try again.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            this.userServiceMock.Setup(service =>
               service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                  .ThrowsAsync(dependencyException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService.ApplyAddAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationDependencyException>(task.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnApplyAddAuditIfServiceErrorOccursAndLogItAsync()
        {
            //Given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();
            var serviceException = new Exception();

            var failedAuditOrchestrationServiceException =
                new FailedAuditOrchestrationServiceException(
                    message: "Failed audit orchestration service error occurred, please contact support.",
                    innerException: serviceException);

            var expectedAuditOrchestrationServiceException =
                new AuditOrchestrationServiceException(
                    message: "Audit orchestration service error occurred, please contact support.",
                    innerException: failedAuditOrchestrationServiceException);

            this.userServiceMock.Setup(service =>
               service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService.ApplyAddAuditValuesAsync(
                somePerson,
                someClaimsPrincipal,
                someSecurityConfiguration);

            AuditOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationServiceException>(task.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAuditOrchestrationServiceException);

            this.userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}
