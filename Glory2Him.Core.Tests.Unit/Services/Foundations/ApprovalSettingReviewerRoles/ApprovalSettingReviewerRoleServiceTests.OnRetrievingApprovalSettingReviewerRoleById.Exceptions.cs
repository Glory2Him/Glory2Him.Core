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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ApprovalSettingReviewerRole> requestEnvelope = CreateRandomApprovalSettingReviewerRoleRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrievingTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onRetrievingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalSettingReviewerRole> requestEnvelope = CreateRandomApprovalSettingReviewerRoleRequestEnvelope();
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrievingTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventAsync()
        {
            // given
            EventEnvelope<ApprovalSettingReviewerRole> requestEnvelope = CreateRandomApprovalSettingReviewerRoleRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                message: "Failed approval setting reviewer role storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: failedStorageApprovalSettingReviewerRoleException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrievingTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleDependencyException actualApprovalSettingReviewerRoleDependencyException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
            actualApprovalSettingReviewerRoleDependencyException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = storageApprovalSettingReviewerRole.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                message: "Failed approval setting reviewer role service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalSettingReviewerRoleServiceException = new ApprovalSettingReviewerRoleServiceException(
                message: "Approval setting reviewer role service error occurred, contact support.",
                innerException: failedApprovalSettingReviewerRoleServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    storageApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalSettingReviewerRole))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrievingTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleServiceException actualApprovalSettingReviewerRoleServiceException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleServiceException>(
                    onRetrievingTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleServiceException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
