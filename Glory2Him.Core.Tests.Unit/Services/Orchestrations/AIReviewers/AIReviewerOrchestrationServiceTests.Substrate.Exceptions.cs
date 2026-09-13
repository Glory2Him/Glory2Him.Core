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
using Glory2Him.Core.Models.Enums;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// §8.6.2.1's failure posture, aimed at the DELIVERY. A failure here is recorded in
    /// <c>EventPublishResult.FailedDeliveries</c> and the publisher decides (§EVN23) — it is
    /// deliberately NOT the logged-and-swallowed posture <c>ResetStaleAIReviewerAssignmentAsync</c>
    /// takes, because that path is the last step of a flow that has already committed and faulting
    /// would report a successful reset as an error, while this one is a whole delivery of its own
    /// with nothing committed behind it.
    ///
    /// <para>Redelivery is safe because gates 3 and 5 make the handler idempotent.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// A token already cancelled abandons the delivery outright, and does so BEFORE the
        /// signature check — there is nothing left to verify for a caller who has gone, and
        /// without it the refused branches would check the token nowhere at all: every gate below
        /// returns without reaching a call that takes one.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldAbandonTheDeliveryWhenTheTokenIsAlreadyCancelledAsync(
            string approvalEventOperation)
        {
            // given: a delivery every gate would otherwise let through
            Guid approvalId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            ValueTask deliveryTask =
                new ValueTask(DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    cancellationTokenSource.Token).AsTask());

            // then: cancellation passes through as ITSELF — a cancelled operation is not a failed
            // one, and the TryCatch only re-shapes a cancellation raised on a token that was NOT
            // cancelled, which is a timeout
            await Assert.ThrowsAnyAsync<OperationCanceledException>(deliveryTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();

            VerifyNoAutomaticAssignmentWasMade();
        }
    }
}
