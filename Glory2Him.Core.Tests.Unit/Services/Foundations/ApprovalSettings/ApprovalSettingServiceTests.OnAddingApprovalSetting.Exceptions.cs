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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingApprovalSettingEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAddingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingApprovalSettingEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingApprovalSettingEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddingApprovalSettingEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                message: "Failed approval setting storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyException actualApprovalSettingDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingApprovalSettingEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();

            var expectedApprovalSettingDependencyValidationException = new ApprovalSettingDependencyValidationException(
                message: "Approval setting dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingDependencyValidationException actualApprovalSettingDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingApprovalSettingEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSetting> requestEnvelope = CreateRandomApprovalSettingRequestEnvelope();
            var serviceException = new Exception();

            var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                message: "Failed approval setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingServiceException = new ApprovalSettingServiceException(
                message: "Approval setting service error occurred, contact support.",
                innerException: failedApprovalSettingServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onAddingTask =
                this.approvalSettingService.OnAddingApprovalSettingAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingServiceException actualApprovalSettingServiceException =
                await Assert.ThrowsAsync<ApprovalSettingServiceException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalSettingServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
