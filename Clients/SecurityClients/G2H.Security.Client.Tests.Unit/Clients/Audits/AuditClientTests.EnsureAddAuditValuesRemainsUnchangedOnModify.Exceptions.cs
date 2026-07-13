// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
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
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(validationException);

            // when
            ValueTask<Person> task = this.auditClient.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditClientValidationException actualAuditClientValidationException =
                await Assert.ThrowsAsync<AuditClientValidationException>(task.AsTask);

            // then
            actualAuditClientValidationException.Should()
                .BeEquivalentTo(expectedAuditClientValidationException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
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
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(dependencyException);

            // when
            ValueTask<Person> task = this.auditClient
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<Person>(
                    somePerson,
                    someStoragePerson,
                    someSecurityConfiguration);

            AuditClientDependencyException actualAuditClientDependencyException =
                await Assert.ThrowsAsync<AuditClientDependencyException>(task.AsTask);

            // then
            actualAuditClientDependencyException.Should().BeEquivalentTo(expectedAuditClientDependencyException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
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
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Person> task = this.auditClient.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                somePerson,
                someStoragePerson,
                someSecurityConfiguration);

            AuditClientServiceException actualAuditClientServiceException =
                await Assert.ThrowsAsync<AuditClientServiceException>(task.AsTask);

            // then
            actualAuditClientServiceException.Should().BeEquivalentTo(expectedAuditClientServiceException);

            this.auditOrchestrationServiceMock.Verify(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Person>(),
                    It.IsAny<Person>(),
                    It.IsAny<SecurityConfigurations>()),
                        Times.Once);

            this.auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
