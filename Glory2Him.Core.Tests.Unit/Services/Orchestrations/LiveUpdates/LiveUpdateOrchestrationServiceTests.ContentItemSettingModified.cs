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
using Glory2Him.Core.Models.Securities;
using Moq;

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
