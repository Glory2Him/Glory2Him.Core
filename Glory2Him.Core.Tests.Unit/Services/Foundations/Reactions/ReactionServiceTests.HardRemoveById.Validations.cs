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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someReactionId = Guid.NewGuid();

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is not authenticated.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

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

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someReactionId = Guid.NewGuid();

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is not allowed to permanently remove this reaction.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

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

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldBlockHardRemoveWhenTheCallerIsGloballyReadOnlyAndLogItAsync()
        {
            // given: the global ReadOnly ban outranks Admin, so a banned Admin is refused before
            // the row is even read — the destructive surface is not an exception to the site-wide
            // contribution ban.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin, Roles.ReadOnly);

            Guid someReactionId = Guid.NewGuid();

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is blocked from contributing reactions.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

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

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
