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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldAddReactionAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateReactionFiller(randomDateTimeOffset).Create();
            Reaction inputReaction = randomReaction;
            Reaction auditAppliedReaction = inputReaction.DeepClone();
            Reaction storageReaction = auditAppliedReaction.DeepClone();
            Reaction expectedReaction = storageReaction.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedReaction.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertReactionAsync(auditAppliedReaction, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(It.IsAny<EventEnvelope<Reaction>>(), ReactionEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Reaction>>(
                        new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.AddReactionAsync(
                    inputReaction,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertReactionAsync(auditAppliedReaction, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
