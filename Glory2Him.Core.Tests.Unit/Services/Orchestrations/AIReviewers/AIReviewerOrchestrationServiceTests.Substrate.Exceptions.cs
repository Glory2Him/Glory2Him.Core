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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Moq;
using Xeptions;

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
    /// <para>Redelivery is safe because gates 3 and 6 make the handler idempotent.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// Criterion 10, downstream failure one of three — the VERDICT READ. The access broker
        /// catches nothing, so a storage outage arrives raw and lands in the service category.
        ///
        /// <para>What this asserts is that it FAULTS THE DELIVERY rather than being swallowed
        /// here, which is what puts it on <c>EventPublishResult.FailedDeliveries</c> instead of
        /// nowhere. Berean not having been asked costs one click by the person who would have
        /// clicked it; a failure nobody can see costs the next reader the whole
        /// investigation.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldReportTheFailedDeliveryWhenTheVerdictReadFailsAsync(
            string approvalEventOperation)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            var verdictReadException = new Exception("storage is unhappy");

            var expectedServiceException =
                new AIReviewerOrchestrationServiceException(
                    message: "AI reviewer orchestration service error occurred, contact support.",

                    innerException: new FailedAIReviewerOrchestrationServiceException(
                        message: "Failed AI reviewer orchestration service error occurred, " +
                            "please contact support.",
                        innerException: verdictReadException,
                        data: verdictReadException.Data));

            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(verdictReadException);

            // when
            ValueTask deliveryTask =
                new ValueTask(DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    TestContext.Current.CancellationToken).AsTask());

            AIReviewerOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationServiceException>(
                    deliveryTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Downstream failure two of three — the PRESENCE READ, on the same posture one gate
        /// further in. Written rather than folded into the test above because the two are
        /// different call sites, and a catch added around one and not the other is exactly the
        /// shape the criterion forbids.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldReportTheFailedDeliveryWhenThePresenceReadFailsAsync(
            string approvalEventOperation)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            var presenceReadException = new Exception("storage is unhappy");

            var expectedServiceException =
                new AIReviewerOrchestrationServiceException(
                    message: "AI reviewer orchestration service error occurred, contact support.",

                    innerException: new FailedAIReviewerOrchestrationServiceException(
                        message: "Failed AI reviewer orchestration service error occurred, " +
                            "please contact support.",
                        innerException: presenceReadException,
                        data: presenceReadException.Data));

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);

            this.accessBrokerMock.Setup(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(presenceReadException);

            // when
            ValueTask deliveryTask =
                new ValueTask(DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    TestContext.Current.CancellationToken).AsTask());

            AIReviewerOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationServiceException>(
                    deliveryTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            VerifyNoAutomaticAssignmentWasMade();
        }

        /// <summary>
        /// Downstream failure three of three — the WORKFLOW WRITE, and criterion 9's rule 2
        /// verified rather than assumed from the registration. The seam's verb runs inside
        /// <c>AIReviewerAssignmentService</c>'s OWN <c>TryCatch</c>, so it throws the same
        /// <c>AIReviewerAssignment*</c> family the caller-facing door does and the arm this
        /// service already carries maps it — ONE arm for two doors, not two (§16.7.1 rule 2).
        ///
        /// <para>Had the seam grown a family of its own, the exception would arrive through the
        /// <c>Xeption</c> catch-all as a plain dependency fault instead, which is the observable
        /// form of the second arm nobody added.</para>
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldReportTheFailedDeliveryWhenTheAutomaticAssignmentWriteFailsAsync(
            string approvalEventOperation)
        {
            // given: the collision UX_AIReviewerAssignments_ApprovalId refuses when two
            // deliveries for one round both pass gate 6 — the concurrency posture #479 ruled
            Guid approvalId = Guid.NewGuid();

            var assignmentWriteException =
                new AIReviewerAssignmentDependencyValidationException(
                    message: "AI reviewer assignment dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: new Xeption(message: "the round already has a live row"));

            var expectedDependencyValidationException =
                new AIReviewerOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: assignmentWriteException.InnerException as Xeption);

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(assignmentWriteException);

            // when
            ValueTask deliveryTask =
                new ValueTask(DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    TestContext.Current.CancellationToken).AsTask());

            AIReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationDependencyValidationException>(
                    deliveryTask.AsTask);

            // then: the losing delivery FAILS and is recorded, which is exactly the posture
            // criterion 10 requires and why no concurrency token is introduced (#479)
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(
                    It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);
        }

        /// <summary>
        /// Cancellation is the one thing that passes through as itself. A cancellation raised
        /// where the token was NOT cancelled is a TIMEOUT — reported as the dependency failure it
        /// is rather than logged raw — and that is the whole of the token-timeout surface, which
        /// the orchestration's existing <c>TryCatch</c> already carries.
        /// </summary>
        [Theory]
        [InlineData(AddedOperation)]
        [InlineData(ModifiedOperation)]
        public async Task ShouldNotSwallowCancellationRaisedByTheAutomaticAssignmentAsync(
            string approvalEventOperation)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);

            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask deliveryTask =
                new ValueTask(DeliverApprovalFactAsync(
                    approvalEventOperation,
                    CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                    TestContext.Current.CancellationToken).AsTask());

            // then
            await Assert.ThrowsAsync<AIReviewerOrchestrationDependencyException>(
                deliveryTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(operationCanceledException),
                Times.Never);
        }

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
