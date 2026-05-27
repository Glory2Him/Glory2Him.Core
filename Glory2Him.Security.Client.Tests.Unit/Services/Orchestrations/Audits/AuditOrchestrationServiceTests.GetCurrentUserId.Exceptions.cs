// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Orchestrations.Audits.Exceptions;
using Glory2Him.Security.Client.Tests.Unit.Models;
using Moq;
using Xeptions;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationOnGetCurrentUserIdIfDependencyValidationOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyValidationException(
                    message: "Audit orchestration dependency validation error occurred, fix the errors and try again.",
                    innerException: dependencyValidationException.InnerException as Xeption);

            this.userServiceMock.Setup(service =>
               service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                   .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<string> task = this.auditOrchestrationService.GetCurrentUserIdAsync(someClaimsPrincipal);

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
        public async Task ShouldThrowDependencyExceptionOnGetCurrentUserIdIfDependencyExceptionOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = CreateRandomClaimsPrincipal();
            var somePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyException(
                    message: "Audit orchestration dependency error occurred, fix the errors and try again.",
                    innerException: dependencyException.InnerException as Xeption);

            this.userServiceMock.Setup(service =>
               service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                  .ThrowsAsync(dependencyException);

            // when
            ValueTask<string> task = this.auditOrchestrationService.GetCurrentUserIdAsync(someClaimsPrincipal);

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
        public async Task ShouldThrowServiceExceptionOnGetCurrentUserIdIfServiceErrorOccursAndLogItAsync()
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
            ValueTask<string> task = this.auditOrchestrationService.GetCurrentUserIdAsync(someClaimsPrincipal);

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