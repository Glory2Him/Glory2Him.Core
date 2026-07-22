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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemovingReactionByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onHardRemovingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnHardRemovingReactionByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutReactionException =
                new TimeoutReactionException(
                    message: "Failed reaction timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: timeoutReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemovingReactionByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHardRemovingReactionByIdEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageReactionException = new FailedStorageReactionException(
                message: "Failed reaction storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: failedStorageReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    onHardRemovingTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemovingReactionByIdEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();

            var expectedReactionDependencyValidationException = new ReactionDependencyValidationException(
                message: "Reaction dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyValidationException actualReactionDependencyValidationException =
                await Assert.ThrowsAsync<ReactionDependencyValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualReactionDependencyValidationException.Should().BeEquivalentTo(
                expectedReactionDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHardRemovingReactionByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var serviceException = new Exception();

            var failedReactionServiceException = new FailedReactionServiceException(
                message: "Failed reaction service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedReactionServiceException = new ReactionServiceException(
                message: "Reaction service error occurred, contact support.",
                innerException: failedReactionServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onHardRemovingTask =
                this.reactionService.OnHardRemovingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionServiceException actualReactionServiceException =
                await Assert.ThrowsAsync<ReactionServiceException>(
                    onHardRemovingTask.AsTask);

            // then
            actualReactionServiceException.Should().BeEquivalentTo(
                expectedReactionServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
