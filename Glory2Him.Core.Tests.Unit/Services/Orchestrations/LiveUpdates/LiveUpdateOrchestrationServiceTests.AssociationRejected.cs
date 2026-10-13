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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.LiveUpdates;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // #901. Association-Rejected takes a reaction out of a count where the entity moves an
    // approved one to Rejected, an override no route reaches; a rejection of an open round moves
    // nothing, and the page's read finds the counts as they were (LiveUpdates.md rule 6). The ear
    // runs the reactions body #900 built, so every case it withholds is withheld here too.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        private const string AssociationRejectedEventName = "AssociationRejected";

        [Fact]
        public async Task ShouldSendACountChangeNamingTheVisibleVersionOnAssociationRejectedAsync()
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> rejectedFact = CreateFactEnvelope(contentItemReaction);

            PublicContentItemGroup visibleVersion =
                CreateVisibleVersion(groupId: contentItemReaction.EntityAGroupId);

            List<ContentItemSettingKey> expectedSettingKeys = CreateSettingKeysFor(visibleVersion);
            LiveUpdate expectedLiveUpdate = CreateReactionsChangeFor(visibleVersion);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    rejectedFact,
                    AssociationRejectedEventName,
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
            await this.liveUpdateOrchestrationService.OnAssociationRejectedAsync(
                rejectedFact,
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
    }
}
