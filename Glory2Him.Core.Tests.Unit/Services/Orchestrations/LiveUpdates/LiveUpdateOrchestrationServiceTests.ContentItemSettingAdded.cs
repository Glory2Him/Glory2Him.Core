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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
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
    }
}
