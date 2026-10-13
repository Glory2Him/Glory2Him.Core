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
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // #898. ContentItemSetting-Modified amends an item's override or a content type's default, so
    // every open page is told the settings it shows may have changed, and reads them again
    // (LiveUpdates.md rules 1 and 5). The ear runs the settings body #897 built.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        // The name EventBroker signs a ContentItemSetting-Modified fact under.
        private const string ContentItemSettingModifiedEventName = "ContentItemSettingModified";

        // Both kinds of row an amendment can touch: a content type's default, which names no
        // item, and an item's override, which names one. Neither is to put an item on the message.
        public static TheoryData<ContentItemSetting> ModifiedSettingRows() =>
            new TheoryData<ContentItemSetting>
            {
                CreateContentItemSettingTypeDefault(),
                CreateContentItemSettingOverride(),
            };

        [Theory]
        [MemberData(nameof(ModifiedSettingRows))]
        public async Task ShouldSendASettingChangeOnContentItemSettingModifiedAsync(
            ContentItemSetting modifiedSetting)
        {
            // given: the delivery comes on a token of its own, so the send is seen to carry the
            // caller's token rather than some other
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            EventEnvelope<ContentItemSetting> modifiedFact =
                CreateSettingFactEnvelope(modifiedSetting);

            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                modifiedFact,
                callerCancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
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
        public async Task ShouldReplyNullToContentItemSettingModifiedAsync()
        {
            // given: nothing replies to a fact (§EVN20). Handing the inbound envelope back would
            // put this service's name on a fact another service published.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> modifiedFact =
                CreateSettingFactEnvelope(settingOverride);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemSetting> actualReply =
                await this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    modifiedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }

        [Fact]
        public async Task ShouldForwardAContentItemSettingModifiedFactCarryingTheSystemIdentityAsync()
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

            EventEnvelope<ContentItemSetting> modifiedFact =
                CreateSettingFactEnvelope(settingOverride, systemSecurityContext);

            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                modifiedFact,
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
        public async Task ShouldRefuseATamperedContentItemSettingModifiedEnvelopeAsync()
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
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingModifiedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    tamperedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingModifiedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    tamperedFact,
                    ContentItemSettingModifiedEventName,
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
        public async Task ShouldRefuseANullContentItemSettingModifiedEnvelopeAsync()
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
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingModifiedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    nullFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingModifiedTask.AsTask);

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
        public async Task ShouldRefuseAContentItemSettingModifiedEnvelopeWithNoContentAsync()
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
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingModifiedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    factWithNoContent,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onContentItemSettingModifiedTask.AsTask);

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
        public async Task ShouldThrowOperationCanceledExceptionOnContentItemSettingModifiedIfTokenIsCancelledAsync()
        {
            // given: a fact that would verify, heard on a token its caller has already cancelled
            var cancelledToken = new CancellationToken(canceled: true);
            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> modifiedFact =
                CreateSettingFactEnvelope(settingOverride);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingModifiedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    modifiedFact,
                    cancelledToken);

            // then: rethrown unwrapped, before the signature is checked
            await Assert.ThrowsAsync<OperationCanceledException>(
                onContentItemSettingModifiedTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A failure is mapped, logged and rethrown as #897's ear maps it, so the substrate records
        // a failed delivery (§EVN11) and nothing retries it (§EVN23 rule 6).
        [Theory]
        [MemberData(nameof(SettingSendFailures))]
        public async Task ShouldRethrowAFailureBeneathContentItemSettingModifiedAndLogItAsync(
            Exception sendException,
            Xeption expectedLiveUpdateOrchestrationException)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> modifiedFact =
                CreateSettingFactEnvelope(settingOverride);

            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            this.liveUpdateServiceMock.Setup(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken))
                        .ThrowsAsync(sendException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>> onContentItemSettingModifiedTask =
                this.liveUpdateOrchestrationService.OnContentItemSettingModifiedAsync(
                    modifiedFact,
                    callerCancellationToken);

            Xeption actualLiveUpdateOrchestrationException =
                await Assert.ThrowsAnyAsync<Xeption>(onContentItemSettingModifiedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationException.Should()
                .BeOfType(expectedLiveUpdateOrchestrationException.GetType());

            actualLiveUpdateOrchestrationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    modifiedFact,
                    ContentItemSettingModifiedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A content type's default: it names no content item.
        private static ContentItemSetting CreateContentItemSettingTypeDefault() =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = VisibleVersionContentType,
                ContentItemId = null,
                CommentsAllowed = true,
                ShowComments = false,
            };
    }
}
