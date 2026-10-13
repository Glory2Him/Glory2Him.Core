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
    // #903. Association-Removed takes a reaction out of its item's counts, whether its reader
    // withdrew it or an administrator took it down, softly or for good (LiveUpdates.md rule 6).
    // The two removals share the address and are told apart by the signed name (§EVN2 rule 4), so
    // the ear hears either. It runs the reactions body #900 built, so every case it withholds is
    // withheld here too.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        private const string AssociationRemovedEventName = "AssociationRemoved";
        private const string AssociationHardRemovedEventName = "AssociationHardRemoved";

        // A soft removal and a hard removal share one address, told apart by the name the
        // publisher signed (§EVN2 rule 4), so a fact signed under either is heard (§EVN15).
        [Theory]
        [InlineData(AssociationRemovedEventName)]
        [InlineData(AssociationHardRemovedEventName)]
        public async Task ShouldSendACountChangeNamingTheVisibleVersionOnAssociationRemovedAsync(
            string signedEventName)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> removedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    signedEventName,
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
            await this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                removedFact,
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
        public async Task ShouldReplyNullToAssociationRemovedAsync()
        {
            // given: nothing replies to a fact (§EVN20)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> removedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    AssociationRemovedEventName,
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
                await this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                    removedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }

        [Fact]
        public async Task ShouldForwardAnAssociationRemovedFactCarryingTheSystemIdentityAsync()
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

            EventEnvelope<Association> removedFact =
                CreateFactEnvelope(contentItemReaction, systemSecurityContext);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    AssociationRemovedEventName,
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
            await this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                removedFact,
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
        public async Task ShouldSendNothingTheReactionsBodyWithholdsOnAssociationRemovedAsync(
            WithheldReactionsCase withheldReactionsCase)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association withheldReaction =
                ArrangeWithheldReactionsCase(withheldReactionsCase, callerCancellationToken);

            EventEnvelope<Association> removedFact = CreateFactEnvelope(withheldReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    AssociationRemovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                removedFact,
                callerCancellationToken);

            // then
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // An envelope that verifies under neither name the address carries is refused before
        // anything is read (§SEC14.8 rule 3, §EVN15 item 10): one this system did not sign, and
        // one it signed for another address.
        [Theory]
        [InlineData(null)]
        [InlineData(AssociationApprovedEventName)]
        public async Task ShouldRefuseAnAssociationRemovedEnvelopeSignedForNeitherNameAsync(
            string signedEventName)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> unverifiedFact = CreateFactEnvelope(contentItemReaction);

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
                    unverifiedFact,
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            if (signedEventName is not null)
            {
                this.envelopeIntegrityBrokerMock.Setup(broker =>
                    broker.VerifyAsync(
                        unverifiedFact,
                        signedEventName,
                        EnvelopeDirection.Request))
                            .ReturnsAsync(true);
            }

            // when
            ValueTask<EventEnvelope<Association>> onAssociationRemovedTask =
                this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                    unverifiedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationRemovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    unverifiedFact,
                    AssociationRemovedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    unverifiedFact,
                    AssociationHardRemovedEventName,
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
        public async Task ShouldRefuseANullAssociationRemovedEnvelopeAsync()
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
            ValueTask<EventEnvelope<Association>> onAssociationRemovedTask =
                this.liveUpdateOrchestrationService.OnAssociationRemovedAsync(
                    nullFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationRemovedTask.AsTask);

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
    }
}
