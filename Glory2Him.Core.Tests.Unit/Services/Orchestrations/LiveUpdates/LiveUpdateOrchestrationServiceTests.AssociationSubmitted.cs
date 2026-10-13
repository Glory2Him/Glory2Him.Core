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
    // #902. Association-Submitted takes an approved reaction out of a count until its round is
    // decided again: an administrator's override re-opens it, or the approval workflow returns a
    // changed reaction's round (§EVN18 rule 1). The approval workflow's ear hears the address too,
    // and neither relies on which runs first (§EVN26 rule 5). The ear runs the reactions body
    // #900 built, so every case it withholds is withheld here too.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        private const string AssociationSubmittedEventName = "AssociationSubmitted";

        [Fact]
        public async Task ShouldSendACountChangeNamingTheVisibleVersionOnAssociationSubmittedAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> submittedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    submittedFact,
                    AssociationSubmittedEventName,
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
            await this.liveUpdateOrchestrationService.OnAssociationSubmittedAsync(
                submittedFact,
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
        public async Task ShouldReplyNullToAssociationSubmittedAsync()
        {
            // given: nothing replies to a fact (§EVN20)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> submittedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    submittedFact,
                    AssociationSubmittedEventName,
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
                await this.liveUpdateOrchestrationService.OnAssociationSubmittedAsync(
                    submittedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }
    }
}
