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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidReactionId = Guid.Empty;

            var invalidReactionException = new InvalidReactionException(
                message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: invalidReactionException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.Reactions.Reaction> retrieveReactionByIdTask =
                this.reactionService.RetrieveReactionByIdAsync(
                    invalidReactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    retrieveReactionByIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfReactionNotFoundAndLogItAsync()
        {
            // given
            Guid someReactionId = Guid.NewGuid();
            Reaction nullReaction = null;

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {someReactionId}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullReaction);

            // when
            ValueTask<Reaction> retrieveReactionByIdTask =
                this.reactionService.RetrieveReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    retrieveReactionByIdTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfReactionIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Reaction storageReaction = CreateRandomReaction();
            storageReaction.IsDeleted = true;
            Guid reactionId = storageReaction.Id;

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            // when
            ValueTask<Reaction> retrieveReactionByIdTask =
                this.reactionService.RetrieveReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    retrieveReactionByIdTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Reaction read denied. Reaction {reactionId} is " +
                        "soft-deleted; reported to the caller as not found."),
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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Reaction storageReaction = CreateRandomReaction();
            storageReaction.IsDeleted = false;
            storageReaction.ApprovalStatus = ApprovalStatus.Draft;
            storageReaction.IsPublished = false;
            Guid reactionId = storageReaction.Id;

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ValueTask<Reaction> retrieveReactionByIdTask =
                this.reactionService.RetrieveReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    retrieveReactionByIdTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Reaction read denied. Reaction {reactionId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotOwnerAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            Reaction storageReaction = CreateRandomReaction();
            storageReaction.IsDeleted = false;
            storageReaction.ApprovalStatus = ApprovalStatus.Draft;
            storageReaction.IsPublished = false;
            Guid reactionId = storageReaction.Id;

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Reaction> retrieveReactionByIdTask =
                this.reactionService.RetrieveReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    retrieveReactionByIdTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Reaction read denied. Reaction {reactionId} " +
                        $"is not publicly visible and user \"{randomActorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
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
