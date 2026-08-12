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
using Glory2Him.Core.Models.Enums;
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
        public async Task ShouldSubmitReactionByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            Reaction storageReaction = CreateSubmittableStorageReaction();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Reaction submittedReaction = storageReaction.DeepClone();
            submittedReaction.ApprovalStatus = ApprovalStatus.Submitted;

            Reaction auditAppliedReaction = submittedReaction.DeepClone();
            Reaction updatedReaction = auditAppliedReaction.DeepClone();
            Reaction expectedReaction = updatedReaction.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupReactionStorageRead(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(
                    auditAppliedReaction,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.SubmitReactionByIdAsync(
                    storageReaction.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectReactionByIdAsync(
                        storageReaction.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        auditAppliedReaction,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ReactionOnSubmittingReactionSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // submit never consults the cross-entity decision — that is the approve's gate
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSubmitReactionByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            Reaction storageReaction = CreateSubmittableStorageReaction();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

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
            await this.reactionService.SubmitReactionByIdAsync(
                storageReaction.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's. Asserting the whole row against the pre-act snapshot, excluding
            // only the one field submit owns, catches any stray write.
            Reaction storageReaction = CreateSubmittableStorageReaction();
            Reaction expectedStorageReaction = storageReaction.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            // when
            Reaction savedReaction = await CaptureSavedReactionOnSubmitAsync(storageReaction);

            // then
            savedReaction.Should().NotBeNull();
            savedReaction.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            savedReaction.Should().BeEquivalentTo(
                expectedStorageReaction,
                options => options.Excluding(reaction => reaction.ApprovalStatus));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            Reaction storageReaction = CreateSubmittableStorageReaction();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            // when
            await CaptureSavedReactionOnSubmitAsync(storageReaction);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Submitted),
                Times.Once);
        }

        // Runs a permitted submit end to end (owner already set up by the caller) and hands back
        // a snapshot of the row that reached the storage broker.
        private async ValueTask<Reaction> CaptureSavedReactionOnSubmitAsync(Reaction storageReaction)
        {
            Reaction savedReaction = null;

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
                        .Callback<Reaction, CancellationToken>(
                            (entity, _) => savedReaction = entity.DeepClone())
                        .ReturnsAsync((Reaction entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    It.IsAny<ReactionEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            await this.reactionService.SubmitReactionByIdAsync(
                storageReaction.Id,
                TestContext.Current.CancellationToken);

            return savedReaction;
        }
    }
}
