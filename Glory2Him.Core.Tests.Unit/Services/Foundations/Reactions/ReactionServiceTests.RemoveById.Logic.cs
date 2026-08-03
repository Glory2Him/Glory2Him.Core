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
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldRemoveReactionByIdAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            randomReaction.IsDeleted = false;
            Reaction storageReaction = randomReaction;

            Reaction auditedReaction = storageReaction.DeepClone();
            auditedReaction.IsDeleted = true;

            Reaction expectedReaction = auditedReaction.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Reaction>>(
                        new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.RemoveReactionByIdAsync(
                    randomReaction.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveReactionByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            Reaction randomReaction = CreateRandomReaction();
            randomReaction.IsDeleted = false;
            Reaction storageReaction = randomReaction;

            Reaction auditedReaction = storageReaction.DeepClone();
            auditedReaction.IsDeleted = true;
            auditedReaction.DeletionReason = someDeletionReason;

            Reaction expectedReaction = auditedReaction.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Reaction>>(
                        new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.RemoveReactionByIdAsync(
                    randomReaction.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnEarlyOnRemoveByIdIfAlreadyDeletedAsync()
        {
            // given
            Reaction alreadyDeletedReaction = CreateRandomReaction();
            alreadyDeletedReaction.IsDeleted = true;
            Guid someReactionId = alreadyDeletedReaction.Id;
            Reaction expectedReaction = alreadyDeletedReaction;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedReaction.CreatedBy);

            // when
            Reaction actualReaction =
                await this.reactionService.RemoveReactionByIdAsync(
                    someReactionId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    someReactionId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveSomeoneElsesReactionByIdWhenUserIsAdminAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomActorUserId = GetRandomString();
            Reaction randomReaction = CreateRandomReaction();
            randomReaction.IsDeleted = false;
            Reaction storageReaction = randomReaction;

            Reaction auditedReaction = storageReaction.DeepClone();
            auditedReaction.IsDeleted = true;

            Reaction expectedReaction = auditedReaction.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Reaction>>(
                        new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.RemoveReactionByIdAsync(
                    randomReaction.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(auditedReaction, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
