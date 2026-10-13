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
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // #904. Association-Restored brings a withdrawn reaction back at the status it was withdrawn
    // at (§DOM4.10 rule 8), so an approved one is counted again with no Association-Approved
    // behind it; this fact is the only announcement of that count moving (LiveUpdates.md rule 6).
    // The ear runs the reactions body #900 built, so every case it withholds is withheld here too.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        private const string AssociationRestoredEventName = "AssociationRestored";

        [Fact]
        public async Task ShouldSendACountChangeNamingTheVisibleVersionOnAssociationRestoredAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> restoredFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
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

            // when
            await this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                restoredFact,
                callerCancellationToken);

            // then
            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyNullToAssociationRestoredAsync()
        {
            // given: nothing replies to a fact (§EVN20)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> restoredFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
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

            // when
            EventEnvelope<Association> actualReply =
                await this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    restoredFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }

        [Fact]
        public async Task ShouldForwardAnAssociationRestoredFactCarryingTheSystemIdentityAsync()
        {
            // given: the fact as a write made under the system identity publishes it. Unlike the
            // workflow's ears, the forwarder does not drop it (§EVN26 rule 3).
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();

            var systemSecurityContext = new SecurityContext
            {
                SubjectId = SystemIdentity.UserId,
                Username = SystemIdentity.Username,
                DelegatedBySubjectId = contentItemReaction.UserId,
                IsAuthenticated = true,
                IsSystemIdentity = true,
                AuthenticationType = AuthenticationType.System,
            };

            EventEnvelope<Association> restoredFact =
                CreateFactEnvelope(contentItemReaction, systemSecurityContext);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
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

            // when
            await this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                restoredFact,
                callerCancellationToken);

            // then
            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A pair that is not a content item's reaction, a host nobody may see, a host whose
        // setting hides its counts, and a host whose setting cannot be resolved: no card shows the
        // change, so nobody is told of it (§SEC14.8 rules 1 and 3).
        [Theory]
        [InlineData(WithheldReactionsCase.EndpointANotAContentItem)]
        [InlineData(WithheldReactionsCase.EndpointBNotAReaction)]
        [InlineData(WithheldReactionsCase.NoVisibleVersion)]
        [InlineData(WithheldReactionsCase.ReactionsHidden)]
        [InlineData(WithheldReactionsCase.SettingUnanswered)]
        public async Task ShouldSendNothingTheReactionsBodyWithholdsOnAssociationRestoredAsync(
            WithheldReactionsCase withheldReactionsCase)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association withheldReaction =
                ArrangeWithheldReactionsCase(withheldReactionsCase, callerCancellationToken);

            EventEnvelope<Association> restoredFact = CreateFactEnvelope(withheldReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                restoredFact,
                callerCancellationToken);

            // then
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // An envelope this system did not sign — or signed for another address, or as a reply —
        // is refused before anything is read (§SEC14.8 rule 3, §EVN15 item 10).
        [Fact]
        public async Task ShouldRefuseATamperedAssociationRestoredEnvelopeAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> tamperedFact = CreateFactEnvelope(contentItemReaction);

            var invalidLiveUpdateOrchestrationException =
                new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "Integrity verification failed.");

            var expectedLiveUpdateOrchestrationValidationException =
                new LiveUpdateOrchestrationValidationException(
                    message: "Live update orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidLiveUpdateOrchestrationException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    tamperedFact,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRestoredTask =
                this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    tamperedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationRestoredTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    tamperedFact,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseANullAssociationRestoredEnvelopeAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;
            EventEnvelope<Association> nullFact = null;

            var invalidLiveUpdateOrchestrationException =
                new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "The fact envelope and its content are required.");

            var expectedLiveUpdateOrchestrationValidationException =
                new LiveUpdateOrchestrationValidationException(
                    message: "Live update orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidLiveUpdateOrchestrationException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRestoredTask =
                this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    nullFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationRestoredTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseAnAssociationRestoredEnvelopeWithNoContentAsync()
        {
            // given: an envelope whose signature would verify, so only the missing content can be
            // what refuses it (§SEC14.6 rule 4)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            var factWithNoContent = new EventEnvelope<Association>
            {
                Content = null,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

            var invalidLiveUpdateOrchestrationException =
                new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "The fact envelope and its content are required.");

            var expectedLiveUpdateOrchestrationValidationException =
                new LiveUpdateOrchestrationValidationException(
                    message: "Live update orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidLiveUpdateOrchestrationException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    factWithNoContent,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRestoredTask =
                this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    factWithNoContent,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationRestoredTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAssociationRestoredIfTokenIsCancelledAsync()
        {
            // given: a fact that would verify, heard on a token its caller has already cancelled
            var cancelledToken = new CancellationToken(canceled: true);
            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> restoredFact = CreateFactEnvelope(contentItemReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRestoredTask =
                this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    restoredFact,
                    cancelledToken);

            // then: rethrown unwrapped, before the signature is checked
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAssociationRestoredTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A failure beneath the ear is mapped, logged and rethrown, so the substrate records a
        // failed delivery (§EVN11). Nothing retries it (§EVN23 rule 6).
        [Theory]
        [InlineData(ReactionsBodyFailure.ContentItemValidation)]
        [InlineData(ReactionsBodyFailure.ContentItemDependencyValidation)]
        [InlineData(ReactionsBodyFailure.ContentItemDependency)]
        [InlineData(ReactionsBodyFailure.ContentItemService)]
        [InlineData(ReactionsBodyFailure.Gather)]
        [InlineData(ReactionsBodyFailure.LiveUpdateValidation)]
        [InlineData(ReactionsBodyFailure.LiveUpdateDependency)]
        [InlineData(ReactionsBodyFailure.LiveUpdateService)]
        [InlineData(ReactionsBodyFailure.Timeout)]
        public async Task ShouldRethrowAFailureBeneathAssociationRestoredAndLogItAsync(
            ReactionsBodyFailure reactionsBodyFailure)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> restoredFact = CreateFactEnvelope(contentItemReaction);

            Xeption expectedLiveUpdateOrchestrationException =
                ArrangeReactionsBodyFailure(
                    contentItemReaction,
                    reactionsBodyFailure,
                    callerCancellationToken);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    restoredFact,
                    AssociationRestoredEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRestoredTask =
                this.liveUpdateOrchestrationService.OnAssociationRestoredAsync(
                    restoredFact,
                    callerCancellationToken);

            Xeption actualLiveUpdateOrchestrationException =
                await Assert.ThrowsAnyAsync<Xeption>(onAssociationRestoredTask.AsTask);

            // then
            actualLiveUpdateOrchestrationException.Should()
                .BeOfType(expectedLiveUpdateOrchestrationException.GetType());

            actualLiveUpdateOrchestrationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
