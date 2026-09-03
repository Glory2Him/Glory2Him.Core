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
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingReactionEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            Reaction storageReaction = CreateSubmittableStorageReaction();

            var requestEnvelope = new EventEnvelope<Reaction>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Reaction { Id = storageReaction.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            SetupReactionStorageRead(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Reaction entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Reaction entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    It.IsAny<ReactionEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            // when
            EventEnvelope<Reaction>? actualReplyEnvelope =
                await this.reactionService.OnSubmittingReactionAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingReactionEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Reaction>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Reaction { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Reaction>? actualReplyEnvelope =
                await this.reactionService.OnSubmittingReactionAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingReactionEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Reaction>? nullEnvelope = null;

            var invalidReactionEventException =
                new InvalidReactionEventException(
                    message: "Invalid reaction event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionEventException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onSubmittingTask =
                this.reactionService.OnSubmittingReactionAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSubmittingReactionEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Reaction>?> onSubmittingTask =
                this.reactionService.OnSubmittingReactionAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onSubmittingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingReactionEventAsync()
        {
            // given
            Reaction storageReaction = CreateApprovableStorageReaction();

            var requestEnvelope = new EventEnvelope<Reaction>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(storageReaction.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupReactionStorageRead(storageReaction);
            SetupAccessBrokerToPermit();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Reaction entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Reaction entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    It.IsAny<ReactionEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            // when
            EventEnvelope<Reaction>? actualReplyEnvelope =
                await this.reactionService.OnApprovingReactionAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingReactionEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Reaction>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Reaction>? actualReplyEnvelope =
                await this.reactionService.OnApprovingReactionAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApprovingReactionEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Reaction>? nullEnvelope = null;

            var invalidReactionEventException =
                new InvalidReactionEventException(
                    message: "Invalid reaction event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionEventException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onApprovingTask =
                this.reactionService.OnApprovingReactionAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedReactionValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnApprovingReactionEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Reaction> requestEnvelope = CreateRandomReactionRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Reaction>?> onApprovingTask =
                this.reactionService.OnApprovingReactionAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onApprovingTask.AsTask);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
