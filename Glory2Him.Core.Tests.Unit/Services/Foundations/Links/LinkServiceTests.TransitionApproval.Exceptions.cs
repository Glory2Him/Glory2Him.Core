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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnTransitionIfCancellationRequestedAsync()
        {
            // given
            Link inputLink = CreateApprovalDecision(Guid.NewGuid());
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                transitionTask.AsTask);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnEnvelopeTransitionIfCancellationRequestedAsync()
        {
            // given: the swap's route checks the token before it chains an envelope, so a
            // cancelled request never mints causation for work it will not do
            Link inputLink = CreateApprovalDecision(Guid.NewGuid());

            EventEnvelope<Link> inboundEnvelope =
                CreateRandomLinkRequestEnvelope(CreateSystemSecurityContext());

            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    inboundEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                transitionTask.AsTask);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateNextAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        It.IsAny<Link>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
