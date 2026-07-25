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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    modifyApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
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
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    modifyApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAsync()
        {
            // given
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyApprovalSettingPublisherRoleTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnModifyIfSqlErrorOccursAndLogItAsync()
        {
            // given
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
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    modifyApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
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
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            ApprovalSettingPublisherRole someApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();

            var expectedApprovalSettingPublisherRoleDependencyValidationException = new ApprovalSettingPublisherRoleDependencyValidationException(
                message: "Approval setting publisher role dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyValidationException actualApprovalSettingPublisherRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyValidationException>(
                    modifyApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
        {
            // given
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
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSettingPublisherRole> modifyApprovalSettingPublisherRoleTask =
                this.approvalSettingPublisherRoleService.ModifyApprovalSettingPublisherRoleAsync(
                    someApprovalSettingPublisherRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleServiceException actualApprovalSettingPublisherRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleServiceException>(
                    modifyApprovalSettingPublisherRoleTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
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
