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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §7.9 rule 6 from THIS side of the move — a review is recorded, and this service re-tests
    /// the round and does nothing else.
    ///
    /// <para>The retirement used to run here as an <c>onVerifiedAsync</c> hook wrapped in an
    /// isolating <c>try</c>/<c>catch</c>, whose whole purpose was that a failure to retire must
    /// never cancel the re-test: a vote that carried a round over the line would otherwise be
    /// counted by nothing, no later event would re-drive it, and the item would sit blocked with
    /// its conditions provably met and nothing on any screen saying why.</para>
    ///
    /// <para><b>That isolation is structural now.</b> Rule 6's retirement is a second
    /// subscription on the same address, in another service, delivered separately and recorded
    /// separately (§EVN11) — so the two tests below assert it by showing that the retirement's
    /// own seams can fault outright without this delivery noticing. They go red if the hook is
    /// ever put back.</para>
    ///
    /// <para>The retirement's own behaviour — who it retires, whom it leaves alone, and that it
    /// acts on no unverified envelope — moved with it to
    /// <c>ApprovalReviewerOrchestrationServiceTests.Substrate.cs</c>.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        private EventEnvelope<ApprovalReview> CreateReviewAddedEnvelope(
            Guid approvalId,
            string createdBy) =>
            new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = approvalId,
                    CreatedBy = createdBy,
                },
                SecurityContext = this.ambientSecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        /// <summary>
        /// The round re-tests and reaches for no invitation at all. Removing the hook does not
        /// change WHEN rule 6 runs — a separate subscription on this service's own address always
        /// runs, and the service it runs on has no suppression window to be ordered against — so
        /// the guarantee the hook's position used to carry is held by construction.
        /// </summary>
        [Fact]
        public async Task ShouldNotReachForAnInvitationWhenAReviewIsRecordedAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, Guid.NewGuid().ToString()),
                TestContext.Current.CancellationToken);

            // then: the round was read, which is the first thing the re-test does
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            // and the reviewer scope was not, because that read belongs to the other delivery
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Retiring the invitation is bookkeeping; re-testing the round is the workflow. A
        /// failure in the first must not cancel the second — and now cannot, because the first
        /// is not on this call stack. Its WRITE seam faults here and nothing about this delivery
        /// changes.
        /// </summary>
        [Fact]
        public async Task ShouldStillReTestTheRoundWhenRetiringTheInvitationFailsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            var retirementFailure =
                new InvalidOperationException("the retirement's own delivery failed");

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementFailure);

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedId.ToString()),
                TestContext.Current.CancellationToken);

            // then: the round was still read, which is the first thing the re-test does
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            // and this delivery neither absorbed the failure nor logged it — it never met it.
            // The retirement's delivery reports its own (§EVN23).
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(retirementFailure),
                Times.Never);
        }

        /// <summary>
        /// The same isolation against rule 8's gathering seam, which is a different read on a
        /// different retirement. Worth its own test rather than folded into the one above: the
        /// two retirements are independent subscriptions with no stated order between them, so a
        /// regression that put either one back on this delivery has to be caught by name.
        /// </summary>
        [Fact]
        public async Task ShouldStillReTestTheRoundWhenTheRetirementLookupFaultsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            // a storage fault, shaped like what the broker really lets through
            var storageFailure = new InvalidOperationException("the database was unavailable");

            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(storageFailure);

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedId.ToString()),
                TestContext.Current.CancellationToken);

            // then
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(storageFailure),
                Times.Never);
        }
    }
}
