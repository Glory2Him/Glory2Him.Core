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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnTransitionApprovalIfCancellationRequestedAsync()
        {
            // given
            ContentItem inputContentItem = CreateApprovalDecision(Guid.NewGuid());
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> transitionTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                transitionTask.AsTask);

            // pins WHERE the guard sits, not merely that it exists. This verb mints an
            // envelope as its first dependency call, so a guard that drifted below that
            // await would still surface OperationCanceledException and still satisfy every
            // assertion below - this is the one that catches the drift.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The internal envelope-carrying overload — the publication swap's own route into the
        // transition, taken directly by ContentItemProcessingService rather than through
        // OnApprovingContentItemAsync. It guards the token itself, before chaining the
        // envelope, so this is the only place that guard is proven: OnApprovingContentItemAsync
        // never calls this overload, it calls the shared do-work directly.
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnEnvelopeTransitionApprovalIfCancellationRequestedAsync()
        {
            // given
            ContentItem inputContentItem = CreateApprovalDecision(Guid.NewGuid());
            EventEnvelope<ContentItem> inboundEnvelope = CreateRandomContentItemRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItem> transitionTask =
                this.contentItemService.TransitionContentItemApprovalAsync(
                    inputContentItem,
                    inboundEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                transitionTask.AsTask);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateNextAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItem>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
