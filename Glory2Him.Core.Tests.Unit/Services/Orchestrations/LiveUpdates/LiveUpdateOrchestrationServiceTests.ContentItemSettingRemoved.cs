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
    // #899. ContentItemSetting-Removed takes an item's override away, softly or for good, so every
    // open page is told the settings it shows may have changed, and the item falls back to its
    // content type's default (LiveUpdates.md rules 1 and 5). A content type's default is never
    // removed (§ARC12.5.2 business rule 5), so every fact here is an override's. The ear runs the
    // settings body #897 built.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        // The two names EventBroker signs a fact on the ContentItemSetting-Removed address under:
        // a soft removal and a hard removal share the address and are told apart by the signed
        // name (§EVN2 rule 4).
        private const string ContentItemSettingRemovedEventName = "ContentItemSettingRemoved";

        private const string ContentItemSettingHardRemovedEventName =
            "ContentItemSettingHardRemoved";

        public static TheoryData<string> RemovedAddressEventNames() =>
            new TheoryData<string>
            {
                ContentItemSettingRemovedEventName,
                ContentItemSettingHardRemovedEventName,
            };

        [Theory]
        [MemberData(nameof(RemovedAddressEventNames))]
        public async Task ShouldSendASettingChangeOnContentItemSettingRemovedAsync(
            string signedEventName)
        {
            // given: the fact verifies under the one name it was signed with and no other, and
            // the delivery comes on a token of its own, so the send is seen to carry the caller's
            // token rather than some other
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> removedFact =
                CreateSettingFactEnvelope(settingOverride);

            LiveUpdate expectedLiveUpdate = CreateSettingChange();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    signedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnContentItemSettingRemovedAsync(
                removedFact,
                callerCancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    removedFact,
                    signedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyNullToContentItemSettingRemovedAsync()
        {
            // given: nothing replies to a fact (§EVN20). Handing the inbound envelope back would
            // put this service's name on a fact another service published.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            ContentItemSetting settingOverride = CreateContentItemSettingOverride();

            EventEnvelope<ContentItemSetting> removedFact =
                CreateSettingFactEnvelope(settingOverride);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    removedFact,
                    ContentItemSettingRemovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemSetting> actualReply =
                await this.liveUpdateOrchestrationService.OnContentItemSettingRemovedAsync(
                    removedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }
    }
}
