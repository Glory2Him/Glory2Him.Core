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
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSubmitIfCancellationRequestedAsync()
        {
            // given
            Guid someReactionId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Reaction> submitReactionTask =
                this.reactionService.SubmitReactionByIdAsync(
                    someReactionId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                submitReactionTask.AsTask);

            // pins WHERE the guard sits, not merely that it exists. This verb mints an
            // envelope as its first dependency call, so a guard that drifted below that
            // await would still surface OperationCanceledException and still satisfy every
            // assertion below - this is the one that catches the drift.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
