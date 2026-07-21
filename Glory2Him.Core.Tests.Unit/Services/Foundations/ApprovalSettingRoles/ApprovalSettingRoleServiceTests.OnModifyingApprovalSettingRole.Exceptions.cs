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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnModifyingApprovalSettingRoleEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
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
        public async Task ShouldThrowDependencyExceptionOnModifyingApprovalSettingRoleEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnModifyingApprovalSettingRoleEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalSettingRoleException =
                new TimeoutApprovalSettingRoleException(
                    message: "Failed approval setting role timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: timeoutApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnModifyingApprovalSettingRoleEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                message: "Failed approval setting role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyException actualApprovalSettingRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyingApprovalSettingRoleEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();

            var expectedApprovalSettingRoleDependencyValidationException = new ApprovalSettingRoleDependencyValidationException(
                message: "Approval setting role dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleDependencyValidationException actualApprovalSettingRoleDependencyValidationException =
                await Assert.ThrowsAsync<ApprovalSettingRoleDependencyValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleDependencyValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnModifyingApprovalSettingRoleEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingRole> requestEnvelope = CreateRandomApprovalSettingRoleRequestEnvelope();
            var serviceException = new Exception();

            var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                message: "Failed approval setting role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingRoleServiceException = new ApprovalSettingRoleServiceException(
                message: "Approval setting role service error occurred, contact support.",
                innerException: failedApprovalSettingRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnModifyingApprovalSettingRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingRole>?> onModifyingTask =
                this.approvalSettingRoleService.OnModifyingApprovalSettingRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingRoleServiceException actualApprovalSettingRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingRoleServiceException>(
                    onModifyingTask.AsTask);

            // then
            actualApprovalSettingRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingRoleServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
