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
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.LiveUpdates
{
    // A failure is mapped, logged and rethrown, so the substrate records a failed delivery
    // (§EVN11). Nothing retries it (§EVN23 rule 6), and the page catches up on its own refresh
    // triggers.
    public partial class LiveUpdateOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAssociationApprovedIfTokenIsCancelledAsync()
        {
            // given: a fact that would verify, heard on a token its caller has already cancelled
            var cancelledToken = new CancellationToken(canceled: true);
            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    approvedFact,
                    AssociationApprovedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(true);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    cancelledToken);

            // then: rethrown unwrapped, before the signature is checked
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAssociationApprovedTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A row whose EntityAGroupId is missing reaches the read as an empty group id, which the
        // read refuses; mapped here, that defect is logged rather than passed over in silence.
        [Theory]
        [MemberData(nameof(ContentItemFailures))]
        public async Task ShouldRethrowAContentItemFailureOnAssociationApprovedAndLogItAsync(
            Xeption contentItemException,
            Xeption expectedLiveUpdateOrchestrationException)
        {
            // given
            using var callerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken callerCancellationToken = callerCancellationTokenSource.Token;

            Association contentItemReaction = CreateContentItemReaction();
            EventEnvelope<Association> approvedFact = CreateFactEnvelope(contentItemReaction);

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
                        .ThrowsAsync(contentItemException);

            // when
            ValueTask<EventEnvelope<Association>> onAssociationApprovedTask =
                this.liveUpdateOrchestrationService.OnAssociationApprovedAsync(
                    approvedFact,
                    callerCancellationToken);

            Xeption actualLiveUpdateOrchestrationException =
                await Assert.ThrowsAnyAsync<Xeption>(onAssociationApprovedTask.AsTask);

            // then
            actualLiveUpdateOrchestrationException.Should()
                .BeOfType(expectedLiveUpdateOrchestrationException.GetType());

            actualLiveUpdateOrchestrationException.Should().BeEquivalentTo(
                expectedLiveUpdateOrchestrationException);

            this.contentItemServiceMock.Verify(service =>
                service.FindPublicContentItemGroupAsync(
                    contentItemReaction.EntityAGroupId,
                    callerCancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(
                    expectedLiveUpdateOrchestrationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.liveUpdateServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
