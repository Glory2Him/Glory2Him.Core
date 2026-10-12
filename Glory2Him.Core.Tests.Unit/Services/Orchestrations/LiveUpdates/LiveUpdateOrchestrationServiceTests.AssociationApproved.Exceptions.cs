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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Foundations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // A failure is mapped, logged and rethrown, so the substrate records a failed delivery
    // (§EVN11). Nothing retries it (§EVN23 rule 6), and the page catches up on its own refresh
    // triggers.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAssociationApprovedIfTokenIsCancelledAsync()
        {
            // given: a fact that would verify, heard on a token its caller has already cancelled
            var cancelledToken = new CancellationToken(canceled: true);
            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    cancelledToken);

            // then: rethrown unwrapped, before the signature is checked
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAssociationApprovedTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A row whose EntityAGroupId is missing reaches the read as an empty group id, which the
        // read refuses; mapped here, that defect is logged rather than passed over in silence.
        [Theory]
        [MemberData(nameof(ContentItemFailures))]
        public async Task ShouldRethrowAContentItemFailureOnAssociationApprovedAndLogItAsync(
            Xeption contentItemException,
            Xeption expectedLiveUpdateOrchestrationException)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken))
                        .ThrowsAsync(contentItemException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            Xeption actualLiveUpdateOrchestrationException =
                await Assert.ThrowsAnyAsync<Xeption>(onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationException.Should()
                .BeOfType(expectedLiveUpdateOrchestrationException.GetType());

            actualLiveUpdateOrchestrationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationException);

            this.contentItemServiceMock.Verify(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A broker raises no family of its own (§ARC12.5), so a failed gather arrives raw and is
        // this service's own service failure.
        [Fact]
        public async Task ShouldRethrowAGatherFailureOnAssociationApprovedAndLogItAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            var gatherException = new Exception(GetRandomString());

            var failedLiveUpdateOrchestrationServiceException =
                new FailedLiveUpdateOrchestrationServiceException(
                    message: "Failed live update orchestration service error occurred, " +
                        "please contact support.",
                    innerException: gatherException,
                    data: gatherException.Data);

            var expectedLiveUpdateOrchestrationServiceException =
                new LiveUpdateOrchestrationServiceException(
                    message: "Live update orchestration service error occurred, contact support.",
                    innerException: failedLiveUpdateOrchestrationServiceException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken))
                        .ReturnsAsync(visibleVersion);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken))
                        .ThrowsAsync(gatherException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationServiceException actualLiveUpdateOrchestrationServiceException =
                await Assert.ThrowsAsync<LiveUpdateOrchestrationServiceException>(
                    onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationServiceException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationServiceException);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationServiceException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowDependencyValidationExceptionOnAssociationApprovedIfTheUpdateIsRefusedAndLogItAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            var liveUpdateValidationException =
                new LiveUpdateValidationException(
                    message: randomMessage,
                    innerException: innerException);

            LiveUpdateOrchestrationDependencyValidationException
                expectedLiveUpdateOrchestrationDependencyValidationException =
                    CreateDependencyValidationException(innerException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken))
                        .ReturnsAsync(visibleVersion);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken))
                        .ReturnsAsync(CreateWinningSettingsFor(visibleVersion, showReactions: true));

            this.liveUpdateServiceMock.Setup(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken))
                        .ThrowsAsync(liveUpdateValidationException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationDependencyValidationException
                actualLiveUpdateOrchestrationDependencyValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationDependencyValidationException>(
                        onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationDependencyValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationDependencyValidationException);

            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationDependencyValidationException))),
                Times.Once);

            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A send that fails is a dependency failure, so the substrate records a failed delivery
        // (§EVN11) and nothing retries it (§EVN23 rule 6).
        [Theory]
        [MemberData(nameof(LiveUpdateFailures))]
        public async Task ShouldRethrowDependencyExceptionOnAssociationApprovedIfTheSendFailsAndLogItAsync(
            Xeption liveUpdateException)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            LiveUpdateOrchestrationDependencyException
                expectedLiveUpdateOrchestrationDependencyException =
                    CreateDependencyException((Xeption)liveUpdateException.InnerException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            this.contentItemServiceMock.Setup(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken))
                        .ReturnsAsync(visibleVersion);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken))
                        .ReturnsAsync(CreateWinningSettingsFor(visibleVersion, showReactions: true));

            this.liveUpdateServiceMock.Setup(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken))
                        .ThrowsAsync(liveUpdateException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationDependencyException
                actualLiveUpdateOrchestrationDependencyException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationDependencyException>(
                        onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationDependencyException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationDependencyException);

            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationDependencyException))),
                Times.Once);

            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
