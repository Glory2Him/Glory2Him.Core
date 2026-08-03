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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingReviewerRoleException =
                new TimeoutApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addApprovalSettingReviewerRoleTask.AsTask);

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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                message: "Failed approval setting reviewer role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var expectedApprovalSettingReviewerRoleDependencyValidationException = new ApprovalSettingReviewerRoleDependencyValidationException(
                message: "Approval setting reviewer role dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyValidationException actualApprovalSettingReviewerRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyValidationException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyValidationException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            var serviceException = new Exception();

            var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                message: "Failed approval setting reviewer role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingReviewerRoleServiceException = new ApprovalSettingReviewerRoleServiceException(
                message: "Approval setting reviewer role service error occurred, contact support.",
                innerException: failedApprovalSettingReviewerRoleServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingReviewerRole> addApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.AddApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleServiceException actualApprovalSettingReviewerRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleServiceException>(
                    addApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
