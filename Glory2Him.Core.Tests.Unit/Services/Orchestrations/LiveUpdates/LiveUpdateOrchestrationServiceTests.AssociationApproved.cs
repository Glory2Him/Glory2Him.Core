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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
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

        [Fact]
        public async Task ShouldSendNothingWhenTheHostHasNoVisibleVersionAsync()
        {
            // given: the host's group has no version every reader may see — a draft, an
            // unpublished or a taken-down item — so no card shows its counts and nobody may read
            // the change (§SEC14.8 rules 1 and 3)
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);
            PublicContentItemGroup noVisibleVersion = null;

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
                        .ReturnsAsync(noVisibleVersion);

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

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSendNothingWhenTheHostsSettingHidesReactionsAsync()
        {
            // given: the visible version's winning setting turns ShowReactions off, so no card
            // shows its counts and nobody may read the change (§SEC14.3 rule 6, §SEC14.8 rule 1)
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
                        .ReturnsAsync(CreateWinningSettingsFor(visibleVersion, showReactions: false));

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

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A key the gather resolves no row for is absent from its answer (IAccessBroker), and a
        // missing setting never means "show": the decision fails closed (§SEC14.8 rule 3). The
        // answer is per key, so a row answering another key — another version, or the version
        // under another content type — is no answer for this one, however it reads.
        [Theory]
        [InlineData(UnresolvedSettingAnswer.NoRow)]
        [InlineData(UnresolvedSettingAnswer.ARowForAnotherVersion)]
        [InlineData(UnresolvedSettingAnswer.ARowOfAnotherContentType)]
        public async Task ShouldSendNothingWhenTheHostsSettingIsUnresolvedAsync(
            UnresolvedSettingAnswer unresolvedSettingAnswer)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);

            List<EffectiveContentItemSetting> unresolvedAnswer =
                CreateUnresolvedAnswerFor(visibleVersion, unresolvedSettingAnswer);

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
                        .ReturnsAsync(unresolvedAnswer);

            // when
            await this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                approvedFact,
                callerCancellationToken);

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    callerCancellationToken),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The forwarder verifies every fact it hears before it reads anything (§SEC14.8 rule 3,
        // §EVN15 item 10). An envelope this system did not sign — or signed for another address,
        // or as a reply — is refused, so a forged fact can neither prompt a read nor reach a page.
        [Fact]
        public async Task ShouldRefuseATamperedAssociationApprovedEnvelopeAsync()
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
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    tamperedFact,
                    callerCancellationToken);

            LiveUpdateOrchestrationValidationException
                actualLiveUpdateOrchestrationValidationException =
                    await Assert.ThrowsAsync<LiveUpdateOrchestrationValidationException>(
                        onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationValidationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    tamperedFact,
                    AssociationApprovedEventName,
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

        public enum UnresolvedSettingAnswer
        {
            NoRow,
            ARowForAnotherVersion,
            ARowOfAnotherContentType
        }

        // Each row that answers another key shows reactions, so only a forwarder that took it
        // for this version's would send.
        private static List<EffectiveContentItemSetting> CreateUnresolvedAnswerFor(
            PublicContentItemGroup visibleVersion,
            UnresolvedSettingAnswer unresolvedSettingAnswer) =>
            unresolvedSettingAnswer switch
            {
                UnresolvedSettingAnswer.ARowForAnotherVersion =>
                    new List<EffectiveContentItemSetting>
                    {
                        CreateEffectiveSetting(
                            contentItemId: Guid.NewGuid(),
                            contentType: visibleVersion.ContentType,
                            showReactions: true),
                    },

                UnresolvedSettingAnswer.ARowOfAnotherContentType =>
                    new List<EffectiveContentItemSetting>
                    {
                        CreateEffectiveSetting(
                            contentItemId: visibleVersion.ContentItemId,
                            contentType: ContentType.Story,
                            showReactions: true),
                    },

                _ => new List<EffectiveContentItemSetting>(),
            };
    }
}
