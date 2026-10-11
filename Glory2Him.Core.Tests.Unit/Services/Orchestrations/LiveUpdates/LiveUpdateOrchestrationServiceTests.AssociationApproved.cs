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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // #900. Association-Approved brings a reaction into a count (LiveUpdates.md rule 6), so every
    // open page showing the host's counts is told they may have moved — and only where everyone
    // may read them (§SEC14.8 rules 1 and 3). Every setup takes the exact values it expects, so
    // the group, the key and the version the forwarder asks for are pinned by the setups alone.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldSendACountChangeNamingTheVisibleVersionOnAssociationApprovedAsync()
        {
            // given: the reader reacted to the version every reader sees, and its winning setting
            // shows reactions. The delivery comes on a token of its own, so the send is seen to
            // carry the caller's token rather than some other.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            contentItemReaction.EntityAKeyId = visibleVersion.ContentItemId;
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);
            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

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

            // when
            await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                approvedFact,
                callerCancellationToken);

            // then
            this.contentItemServiceMock.Verify(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken),
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
        public async Task ShouldNameTheGroupsVisibleVersionNotTheReactedVersionAsync()
        {
            // given: the reader reacted to an earlier version of the host. Only the group's
            // canonically visible version has a card showing counts (§ARC16.8), so that is the
            // version the message names, found by the row's group rather than by its key.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

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

            // when
            await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                approvedFact,
                callerCancellationToken);

            // then
            this.liveUpdateServiceMock.Verify(service =>
                service.SendLiveUpdateAsync(
                    It.Is(SameLiveUpdateAs(expectedLiveUpdate)),
                    callerCancellationToken),
                Times.Once);

            this.liveUpdateServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldForwardAnAssociationApprovedFactCarryingTheSystemIdentityAsync()
        {
            // given: the fact as the approval workflow's own decision publishes it — under the
            // seeded tier, the moment a reader gives a reaction. Unlike the workflow's ears, the
            // forwarder does not drop its writes: an approval is exactly the change a page has to
            // hear (§EVN26 rule 3).
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

            EventEnvelope<Association> approvedFact =
                CreateFactEnvelope(contentItemReaction, systemSecurityContext);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

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

            // when
            await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                approvedFact,
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
        public async Task ShouldReplyNullToAssociationApprovedAsync()
        {
            // given: nothing replies to a fact (§EVN20). Handing the inbound envelope back would
            // put this service's name on a fact another service published.
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

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

            // when
            EventEnvelope<Association> actualReply =
                await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            // then
            actualReply.Should().BeNull();
        }

        // Only a reaction on a content item is counted (§ARC16.8, Which rows are counted), so only
        // that pair can move a count a card shows. Each end wrong on its own, and both wrong.
        [Theory]
        [InlineData(EntityType.ContentItem, EntityType.Tag)]
        [InlineData(EntityType.Comment, EntityType.Reaction)]
        [InlineData(EntityType.Reaction, EntityType.Tag)]
        [InlineData(EntityType.Reaction, EntityType.ContentItem)]
        public async Task ShouldSendNothingForAPairThatIsNotAContentItemsReactionAsync(
            EntityType entityAType,
            EntityType entityBType)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association otherPair = CreateContentItemReaction();
            otherPair.EntityAType = entityAType;
            otherPair.EntityBType = entityBType;
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(otherPair);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                approvedFact,
                callerCancellationToken);

            // then: nothing read, nothing gathered, nothing sent
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
