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
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Reaction someReaction = CreateRandomReaction();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    addReactionTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
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
            Reaction someReaction = CreateRandomReaction();

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    addReactionTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
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
            Reaction someReaction = CreateRandomReaction();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                addReactionTask.AsTask);

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
            Reaction someReaction = CreateRandomReaction();
            SqlException sqlException = GetSqlException();

            var failedStorageReactionException = new FailedStorageReactionException(
                message: "Failed reaction storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: failedStorageReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    addReactionTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
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
            Reaction someReaction = CreateRandomReaction();

            var expectedReactionDependencyValidationException = new ReactionDependencyValidationException(
                message: "Reaction dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    TestContext.Current.CancellationToken);

            ReactionDependencyValidationException actualReactionDependencyValidationException =
                await Assert.ThrowsAsync<ReactionDependencyValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionDependencyValidationException.Should().BeEquivalentTo(
                expectedReactionDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyValidationException))),
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
            Reaction someReaction = CreateRandomReaction();
            var serviceException = new Exception();

            var failedReactionServiceException = new FailedReactionServiceException(
                message: "Failed reaction service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedReactionServiceException = new ReactionServiceException(
                message: "Reaction service error occurred, contact support.",
                innerException: failedReactionServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    someReaction,
                    TestContext.Current.CancellationToken);

            ReactionServiceException actualReactionServiceException =
                await Assert.ThrowsAsync<ReactionServiceException>(
                    addReactionTask.AsTask);

            // then
            actualReactionServiceException.Should().BeEquivalentTo(
                expectedReactionServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(someReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
