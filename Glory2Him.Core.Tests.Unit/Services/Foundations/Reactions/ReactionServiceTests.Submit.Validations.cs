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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.UpsertDataList(
                key: nameof(Reaction.Id),
                value: "Id is required");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectReactionByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectReactionByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ReactionReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedReactionException =
                new UnauthorizedReactionException(
                    message: "The current user is blocked from contributing reactions.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectReactionByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid reactionId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    reactionId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Reaction)null);

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {reactionId}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    reactionId,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Reaction storageReaction = CreateSubmittableStorageReaction();
            storageReaction.IsDeleted = true;

            SetupReactionStorageRead(storageReaction);

            var notFoundReactionException =
                new NotFoundReactionException(
                    message: $"Reaction not found with id: {storageReaction.Id}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: notFoundReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    storageReaction.Id,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A Reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Reaction storageReaction = CreateSubmittableStorageReaction();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupReactionStorageRead(storageReaction);

            var unauthorizedReactionException =
                new UnauthorizedReactionException(
                    message: "The current user is not allowed to submit this reaction.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    storageReaction.Id,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfTheStoredRowIsNotDraftAsync(
            ApprovalStatus storageStatus)
        {
            // given: only a Draft may be submitted (issue #111 case 7). A row already Submitted
            // or Approved is not a fresh submission — re-submitting one would either re-open a
            // decided item or re-announce a pending one. The caller is the owner, so this proves
            // the state gate stands on its own, after authorization passes.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Reaction storageReaction = CreateSubmittableStorageReaction();
            storageReaction.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            SetupReactionStorageRead(storageReaction);

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            // when
            ValueTask<Reaction> submitTask =
                this.reactionService.SubmitReactionByIdAsync(
                    storageReaction.Id,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        It.IsAny<ReactionEventOperation>()),
                Times.Never);
        }
    }
}
