// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// §7.9 rule 8's failure posture, re-aimed at the DELIVERY. What these assert has not changed
    /// — a failure in either retirement never faults whoever pressed Approve — but where it is
    /// enforced has moved, and that is the point of keeping them rather than deleting them as
    /// redundant.
    ///
    /// <para>Before the move a hand-written <c>try</c>/<c>catch</c>/log inside the orchestration
    /// held the guarantee, repeated at each site that could close a round. Now the retirement is
    /// a separate delivery: it faults its own handler, the substrate records that delivery as
    /// failed on the publish result, and the publisher decides (§EVN23). The reason is sharper
    /// than tidiness — by the time this runs the decision has COMMITTED, so faulting the caller
    /// would report a decision that fully worked as an error, and the retry it advises is then
    /// refused because the round is no longer <c>Submitted</c>.</para>
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// The read is the gathering seam and the broker catches nothing, so a storage outage
        /// arrives raw rather than as one of the <c>ApprovalReviewRequest</c> families.
        /// </summary>
        [Fact]
        public async Task ShouldReportTheFailedDeliveryWhenTheRetirementReadFailsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            var retirementReadException = new Exception("storage is unhappy");

            var expectedServiceException =
                new ApprovalReviewerOrchestrationServiceException(
                    message: "Approval reviewer orchestration service error occurred, " +
                        "contact support.",

                    innerException: new FailedApprovalReviewerOrchestrationServiceException(
                        message: "Failed approval reviewer orchestration service error occurred, " +
                            "please contact support.",
                        innerException: retirementReadException,
                        data: retirementReadException.Data));

            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementReadException);

            // when
            ValueTask deliveryTask =
                new ValueTask(this.approvalReviewerOrchestrationService
                    .OnApprovalModifiedAsync(
                        CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                        TestContext.Current.CancellationToken).AsTask());

            ApprovalReviewerOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationServiceException>(
                    deliveryTask.AsTask);

            // then: it faults the DELIVERY rather than being swallowed here, which is what puts
            // it on EventPublishResult.FailedDeliveries instead of nowhere
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);
        }

        /// <summary>
        /// The same posture one layer in: the read answered and the WRITE failed. The rows stay
        /// outstanding and a moderator can still withdraw them by hand, which is a smaller cost
        /// than faulting a decision that fully worked.
        /// </summary>
        [Fact]
        public async Task ShouldReportTheFailedDeliveryWhenARetirementWriteFailsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid requestId = Guid.NewGuid();

            var retirementWriteException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, "
                        + "contact support.",
                    innerException: new Xeption(message: "storage is unhappy"));

            var expectedDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: "Approval reviewer orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: retirementWriteException.InnerException as Xeption);

            SetupRetirableApprovalReviewRequests(approvalId, requestId);

            this.approvalReviewRequestWorkflowServiceMock.Setup(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementWriteException);

            // when
            ValueTask deliveryTask =
                new ValueTask(this.approvalReviewerOrchestrationService
                    .OnApprovalModifiedAsync(
                        CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                        TestContext.Current.CancellationToken).AsTask());

            ApprovalReviewerOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyException>(
                    deliveryTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// Cancellation is the one thing that passes through as itself. A cancelled operation is
        /// not a failed one, and a cancellation raised where the token was NOT cancelled is a
        /// timeout — reported as the dependency failure it is rather than logged raw.
        /// </summary>
        [Fact]
        public async Task ShouldNotSwallowCancellationRaisedByTheRetirementAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask deliveryTask =
                new ValueTask(this.approvalReviewerOrchestrationService
                    .OnApprovalModifiedAsync(
                        CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                        TestContext.Current.CancellationToken).AsTask());

            // then
            await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyException>(
                deliveryTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(operationCanceledException),
                Times.Never);
        }

        /// <summary>
        /// A token already cancelled abandons the delivery outright, and does so BEFORE the
        /// signature check — there is nothing left to verify for a caller who has gone, and
        /// without this the refused branch would check the token nowhere.
        /// </summary>
        [Fact]
        public async Task ShouldAbandonTheDeliveryWhenTheTokenIsAlreadyCancelledAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            ValueTask deliveryTask =
                new ValueTask(this.approvalReviewerOrchestrationService
                    .OnApprovalModifiedAsync(
                        CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                        cancellationTokenSource.Token).AsTask());

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(deliveryTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
