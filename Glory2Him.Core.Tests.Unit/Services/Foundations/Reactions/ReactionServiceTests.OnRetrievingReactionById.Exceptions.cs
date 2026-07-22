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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingReactionByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrievingTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
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
        public async Task ShouldThrowDependencyExceptionOnRetrievingReactionByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
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
                broker.SelectReactionByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrievingTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingReactionByIdEventAsync()
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
                broker.SelectReactionByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrievingTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
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

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingReactionByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Reaction storageReaction = CreateRandomReaction();
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<Reaction>
            {
                Content = new Reaction { Id = storageReaction.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedReactionServiceException = new FailedReactionServiceException(
                message: "Failed reaction service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedReactionServiceException = new ReactionServiceException(
                message: "Reaction service error occurred, contact support.",
                innerException: failedReactionServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    storageReaction.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageReaction);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateNextAsync(requestEnvelope, storageReaction))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrievingTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionServiceException actualReactionServiceException =
                await Assert.ThrowsAsync<ReactionServiceException>(
                    onRetrievingTask.AsTask);

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
