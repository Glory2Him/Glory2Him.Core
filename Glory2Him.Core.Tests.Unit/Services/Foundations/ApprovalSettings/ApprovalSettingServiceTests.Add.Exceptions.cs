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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingException =
                new TimeoutApprovalSettingException(
                    message: "Failed approval setting timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addApprovalSettingTask.AsTask);

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                message: "Failed approval setting storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();

            var expectedApprovalSettingDependencyValidationException = new ApprovalSettingDependencyValidationException(
                message: "Approval setting dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyValidationException actualApprovalSettingDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyValidationException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalSetting someApprovalSetting = CreateRandomApprovalSetting();
            var serviceException = new Exception();

            var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                message: "Failed approval setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingServiceException = new ApprovalSettingServiceException(
                message: "Approval setting service error occurred, contact support.",
                innerException: failedApprovalSettingServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    someApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingServiceException actualApprovalSettingServiceException =
                await Assert.ThrowsAsync<ApprovalSettingServiceException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
