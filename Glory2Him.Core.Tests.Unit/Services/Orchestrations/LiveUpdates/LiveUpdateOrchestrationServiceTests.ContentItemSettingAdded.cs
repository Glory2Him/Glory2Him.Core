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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // #897. ContentItemSetting-Added switches something on or off for one item, so every open
    // page is told the settings it shows may have changed, and reads them again (LiveUpdates.md
    // rules 1 and 5). Every setting row is public-read, so nothing is read first (§SEC14.7
    // posture C rule 2).
    public partial class LiveUpdateOrchestrationServiceTests
    {
        // The name EventBroker signs a ContentItemSetting-Added fact under.
        private const string ContentItemSettingAddedEventName = "ContentItemSettingAdded";

        [Fact]
        public async Task ShouldSendASettingChangeOnContentItemSettingAddedAsync()
        {
            // given: the delivery comes on a token of its own, so the send is seen to carry the
            // caller's token rather than some other
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();
            EventEnvelope<ContentItemSetting> addedFact = CreateSettingFactEnvelope(settingOverride);
            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    addedFact,
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                addedFact,
                callerCancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    addedFact,
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyNullToContentItemSettingAddedAsync()
        {
            // given: nothing replies to a fact (§EVN20). Handing the inbound envelope back would
            // put this service's name on a fact another service published.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();
            EventEnvelope<ContentItemSetting> addedFact = CreateSettingFactEnvelope(settingOverride);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    addedFact,
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemSetting> actualReply =
                await this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                    addedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }

        [Fact]
        public async Task ShouldForwardAContentItemSettingAddedFactCarryingTheSystemIdentityAsync()
        {
            // given: the fact as a system write publishes it. Unlike the approval workflow's
            // ears, the forwarder does not drop it: a page has to hear every change to what it
            // shows, whoever made it (§EVN26 rule 3).
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            var systemSecurityContext = new SecurityContext
            {
                SubjectId = SystemIdentity.UserId,
                Username = SystemIdentity.Username,
                IsAuthenticated = true,
                IsSystemIdentity = true,
                AuthenticationType = AuthenticationType.System,
            };

            EventEnvelope<ContentItemSetting> addedFact =
                CreateSettingFactEnvelope(settingOverride, systemSecurityContext);

            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    addedFact,
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                addedFact,
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

        // The forwarder verifies every fact it hears before it does anything else (§EVN15 item
        // 10). An envelope this system did not sign — or signed for another address, or as a
        // reply — is refused, so a forged fact cannot reach a page.
        [Fact]
        public async Task ShouldRefuseATamperedContentItemSettingAddedEnvelopeAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> tamperedFact =
                CreateSettingFactEnvelope(settingOverride);

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
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingAddedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                    tamperedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingAddedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    tamperedFact,
                    ContentItemSettingAddedEventName,
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

        // An envelope with nothing to verify, or nothing in it to forward, is refused before the
        // signature is asked.
        [Fact]
        public async Task ShouldRefuseANullContentItemSettingAddedEnvelopeAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;
            EventEnvelope<ContentItemSetting> nullFact = null;

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
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingAddedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                    nullFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingAddedTask.AsTask);

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
        public async Task ShouldRefuseAContentItemSettingAddedEnvelopeWithNoContentAsync()
        {
            // given: an envelope whose signature would verify, so only the missing content can be
            // what refuses it — a signature proves provenance, never that a section is present
            // (§SEC14.6 rule 4)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            var factWithNoContent = new EventEnvelope<ContentItemSetting>
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
                    ContentItemSettingAddedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingAddedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingAddedAsync(
                    factWithNoContent,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingAddedTask.AsTask);

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
