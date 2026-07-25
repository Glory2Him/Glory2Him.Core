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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnModifyingApprovalSettingPublisherRoleEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onModifyingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyingApprovalSettingPublisherRoleEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnModifyingApprovalSettingPublisherRoleEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnModifyingApprovalSettingPublisherRoleEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                message: "Failed approval setting publisher role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingPublisherRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyException actualApprovalSettingPublisherRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyingApprovalSettingPublisherRoleEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();

            var expectedApprovalSettingPublisherRoleDependencyValidationException = new ApprovalSettingPublisherRoleDependencyValidationException(
                message: "Approval setting publisher role dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleDependencyValidationException actualApprovalSettingPublisherRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleDependencyValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnModifyingApprovalSettingPublisherRoleEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole> requestEnvelope = CreateRandomApprovalSettingPublisherRoleRequestEnvelope();
            var serviceException = new Exception();

            var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                message: "Failed approval setting publisher role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingPublisherRoleServiceException = new ApprovalSettingPublisherRoleServiceException(
                message: "Approval setting publisher role service error occurred, contact support.",
                innerException: failedApprovalSettingPublisherRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onModifyingTask =
                this.approvalSettingPublisherRoleService.OnModifyingApprovalSettingPublisherRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleServiceException actualApprovalSettingPublisherRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleServiceException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
