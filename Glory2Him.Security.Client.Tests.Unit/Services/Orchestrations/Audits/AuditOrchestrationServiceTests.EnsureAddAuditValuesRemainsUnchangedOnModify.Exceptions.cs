// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
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
        public async Task ShouldThrowDependencyValidationOnEnsureAddAuditIfDependencyValidationOccursAndLogItAsync(
            Xeption dependencyValidationException)
        {
            // given
            var somePerson = new Person { Name = GetRandomString() };
            var someStoragePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyValidationException(
                    message: "Audit orchestration dependency validation error occurred, fix the errors and try again.",
                    innerException: dependencyValidationException.InnerException as Xeption);

            this.auditServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(dependencyValidationException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationDependencyValidationException>(task.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedDependencyException);

            this.auditServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                    Times.Once);

            this.auditServiceMock.VerifyNoOtherCalls();
            this.userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnEnsureAddAuditIfDependencyExceptionOccursAndLogItAsync(
            Xeption dependencyException)
        {
            // given
            var somePerson = new Person { Name = GetRandomString() };
            var someStoragePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedDependencyException =
                new AuditOrchestrationDependencyException(
                    message: "Audit orchestration dependency error occurred, fix the errors and try again.",
                    innerException: dependencyException.InnerException as Xeption);

            this.auditServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService
                .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<Person>(
                    somePerson,
                    someStoragePerson,
                    someSecurityConfiguration);

            AuditOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationDependencyException>(task.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.auditServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                    Times.Once);

            this.auditServiceMock.VerifyNoOtherCalls();
            this.userServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnEnsureAddAuditIfServiceErrorOccursAndLogItAsync()
        {
            //Given
            var somePerson = new Person { Name = GetRandomString() };
            var someStoragePerson = new Person { Name = GetRandomString() };
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

            this.auditServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Person> task = this.auditOrchestrationService.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AuditOrchestrationServiceException>(task.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAuditOrchestrationServiceException);

            this.auditServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                    Times.Once);

            this.auditServiceMock.VerifyNoOtherCalls();
            this.userServiceMock.VerifyNoOtherCalls();
        }
    }
}