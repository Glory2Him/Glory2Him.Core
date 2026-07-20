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
using G2H.Security.Client.Models.Clients.Audits.Exceptions;
using Moq;
using Xeptions;

namespace G2H.Security.Client.Tests.Clients.Audits
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
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

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
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

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
