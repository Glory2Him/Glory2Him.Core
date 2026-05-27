// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients.Audits.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnGetUserIdIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedAuditClientValidationException =
                new AuditClientValidationException(
                    message: "Audit client validation error occurred, fix the error and try again.",
                    innerException: validationException.InnerException as Xeption,
                    data: validationException.InnerException.Data);

            auditOrchestrationServiceMock.Setup(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(validationException);

            // when
            ValueTask<string> getUserIdTask =
                auditClient.GetUserIdAsync(someClaimsPrincipal);

            AuditClientValidationException actualAuditClientValidationException =
                await Assert.ThrowsAsync<AuditClientValidationException>(
                    getUserIdTask.AsTask);

            // then
            actualAuditClientValidationException.Should()
                .BeEquivalentTo(expectedAuditClientValidationException);

            auditOrchestrationServiceMock.Verify(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnGetUserIdIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedAuditClientDependencyException =
                new AuditClientDependencyException(
                    message: "Audit client dependency error occurred, please contact support.",
                    innerException: dependencyException.InnerException as Xeption,
                    data: dependencyException.InnerException.Data);

            auditOrchestrationServiceMock.Setup(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(dependencyException);

            // when
            ValueTask<string> getUserIdTask =
                auditClient.GetUserIdAsync(someClaimsPrincipal);

            AuditClientDependencyException actualAuditClientDependencyException =
                await Assert.ThrowsAsync<AuditClientDependencyException>(getUserIdTask.AsTask);

            // then
            actualAuditClientDependencyException.Should()
                .BeEquivalentTo(expectedAuditClientDependencyException);

            auditOrchestrationServiceMock.Verify(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task ShouldThrowServiceExceptionOnGetUserIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            var serviceException = new Exception(message: GetRandomString());

            var expectedAuditClientServiceException =
                new AuditClientServiceException(
                    message: "Audit client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            auditOrchestrationServiceMock.Setup(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(serviceException);

            // when
            ValueTask<string> getUserIdTask =
                auditClient.GetUserIdAsync(someClaimsPrincipal);

            AuditClientServiceException actualAuditClientServiceException =
                await Assert.ThrowsAsync<AuditClientServiceException>(
                    getUserIdTask.AsTask);

            // then
            actualAuditClientServiceException.Should()
                .BeEquivalentTo(expectedAuditClientServiceException);

            auditOrchestrationServiceMock.Verify(service =>
                service.GetCurrentUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            auditOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
