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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingPublisherRoleException =
                new TimeoutApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddIfCancellationRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addApprovalSettingPublisherRoleTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                message: "Failed approval setting publisher role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingPublisherRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();

            var expectedApprovalSettingPublisherRoleDependencyValidationException = new ApprovalSettingPublisherRoleDependencyValidationException(
                message: "Approval setting publisher role dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyValidationException actualApprovalSettingPublisherRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyValidationException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            var serviceException = new Exception();

            var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                message: "Failed approval setting publisher role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingPublisherRoleServiceException = new ApprovalSettingPublisherRoleServiceException(
                message: "Approval setting publisher role service error occurred, contact support.",
                innerException: failedApprovalSettingPublisherRoleServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingPublisherRole> addApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.AddApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleServiceException actualApprovalSettingPublisherRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleServiceException>(
                    addApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
