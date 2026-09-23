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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // THE ADDING HANDLER'S OWN DEDUPLICATION QUESTION, asked on its own (#631). Same receiver
        // name and same storage probe OnAddingAssociationAsync uses, so the orchestration asking
        // first and the handler asking again cannot answer differently — a repeated read, not a
        // second rule. Both answers are pinned, so a constant cannot pass.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldAnswerWhetherTheAddingRequestWasAlreadyAppliedAsync(
            bool alreadyProcessed)
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyProcessed);

            // when
            bool actualAnswer =
                await this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualAnswer.Should().Be(alreadyProcessed);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // #631 criterion 5b. Asked by the orchestration AHEAD of every endpoint read, so a caller
        // that has already cancelled must not pay for the storage round trip either.
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnHasAlreadyAddedAssociationIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<bool> hasAlreadyAddedTask =
                this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hasAlreadyAddedTask.AsTask);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHasAlreadyAddedAssociationIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException =
                new FailedStorageAssociationException(
                    message: "Failed content item association storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedAssociationDependencyException =
                new AssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: failedStorageAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<bool> hasAlreadyAddedTask =
                this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    hasAlreadyAddedTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnHasAlreadyAddedAssociationIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationException =
                new TimeoutAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAssociationDependencyException =
                new AssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: timeoutAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<bool> hasAlreadyAddedTask =
                this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualAssociationDependencyException =
                await Assert.ThrowsAsync<AssociationDependencyException>(
                    hasAlreadyAddedTask.AsTask);

            // then
            actualAssociationDependencyException.Should().BeEquivalentTo(
                expectedAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHasAlreadyAddedAssociationIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var serviceException = new Exception();

            var failedAssociationServiceException =
                new FailedAssociationServiceException(
                    message: "Failed content item association service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedAssociationServiceException =
                new AssociationServiceException(
                    message: "Content item association service error occurred, contact support.",
                    innerException: failedAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<bool> hasAlreadyAddedTask =
                this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualAssociationServiceException =
                await Assert.ThrowsAsync<AssociationServiceException>(
                    hasAlreadyAddedTask.AsTask);

            // then
            actualAssociationServiceException.Should().BeEquivalentTo(
                expectedAssociationServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationServiceException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
