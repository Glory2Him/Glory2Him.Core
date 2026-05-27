// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Clients.Audits.Exceptions;
using Glory2Him.Security.Client.Tests.Unit.Models;
using Moq;
using Xeptions;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowDependencyValidationOnEnsureAddAuditIfDependencyValidationOccursAndLogItAsync(
            Xeption validationException)
        {
            // given
            var somePerson = new Person { Name = GetRandomString() };
            var someStoragePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();

            var expectedAuditClientValidationException =
                new AuditClientValidationException(
                    message: "Audit client validation error occurred, fix the error and try again.",
                    innerException: validationException.InnerException as Xeption,
                    data: validationException.InnerException.Data);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(validationException);

            // when
            ValueTask<Person> task = this.auditClient.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditClientValidationException actualAuditClientValidationException =
                await Assert.ThrowsAsync<AuditClientValidationException>(task.AsTask);

            // then
            actualAuditClientValidationException.Should()
                .BeEquivalentTo(expectedAuditClientValidationException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                    Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
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

            var expectedAuditClientDependencyException =
                new AuditClientDependencyException(
                    message: "Audit client dependency error occurred, please contact support.",
                    innerException: dependencyException.InnerException as Xeption,
                    data: dependencyException.InnerException.Data);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<Person> task = this.auditClient
                .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<Person>(
                    somePerson,
                    someStoragePerson,
                    someSecurityConfiguration);

            AuditClientDependencyException actualAuditClientDependencyException =
                await Assert.ThrowsAsync<AuditClientDependencyException>(task.AsTask);

            // then
            actualAuditClientDependencyException.Should().BeEquivalentTo(expectedAuditClientDependencyException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnEnsureAddAuditIfServiceErrorOccursAndLogItAsync()
        {
            //Given
            var somePerson = new Person { Name = GetRandomString() };
            var someStoragePerson = new Person { Name = GetRandomString() };
            var someSecurityConfiguration = GetSecurityConfigurations();
            var serviceException = new Exception();

            var expectedAuditClientServiceException =
                new AuditClientServiceException(
                    message: "Audit client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Person> task = this.auditClient.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditClientServiceException actualAuditClientServiceException =
                await Assert.ThrowsAsync<AuditClientServiceException>(task.AsTask);

            // then
            actualAuditClientServiceException.Should().BeEquivalentTo(expectedAuditClientServiceException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}