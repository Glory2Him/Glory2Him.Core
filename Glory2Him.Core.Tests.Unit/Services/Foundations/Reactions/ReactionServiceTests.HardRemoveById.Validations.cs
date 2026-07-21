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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidReactionId = Guid.Empty;

            var invalidReactionException = new InvalidReactionException(
                message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.UpsertDataList(
                key: nameof(Reaction.Id),
                value: "Id is required");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: invalidReactionException);

            // when
            ValueTask<Reaction> hardRemoveReactionByIdTask =
                this.reactionService.HardRemoveReactionByIdAsync(
                    invalidReactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    hardRemoveReactionByIdTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfReactionNotFoundAndLogItAsync()
        {
            // given
            Guid someReactionId = Guid.NewGuid();
            Reaction noReaction = null;

            var notFoundReactionException = new NotFoundReactionException(
                message: $"Reaction not found with id: {someReactionId}.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: notFoundReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noReaction);

            // when
            ValueTask<Reaction> hardRemoveReactionByIdTask =
                this.reactionService.HardRemoveReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    hardRemoveReactionByIdTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
