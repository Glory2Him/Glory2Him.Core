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
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        private const string ExpectedRetirementReason =
            "Retired: the invited reviewer recorded their review.";

        /// <summary>
        /// §7.9 rule 6 — the invited person answered, so the invitation retires itself. It runs
        /// under the SYSTEM identity, which is why it cannot go through the public withdraw verb:
        /// <c>CreateSystemAsync</c> mints a context carrying no roles, and the withdraw gate asks
        /// for a review-tier role. This test is the proof that rule 6 has a route at all.
        /// </summary>
        [Fact]
        public async Task ShouldRetireAnsweredApprovalReviewRequestUnderTheSystemIdentityAsync()
        {
            // given: the caller holds NO review role — the system identity is the authority here
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();
            auditAppliedApprovalReviewRequest.IsDeleted = true;
            auditAppliedApprovalReviewRequest.DeletionReason = ExpectedRetirementReason;
            ApprovalReviewRequest retiredApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = retiredApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageApprovalReviewRequest,
                    It.Is<SecurityContext>(context => context.IsSystemIdentity),
                    ExpectedRetirementReason))
                        .ReturnsAsync(auditAppliedApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retiredApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Removed))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestWorkflowService
                    .RetireAnsweredApprovalReviewRequestAsync(
                        inputApprovalReviewRequestId,
                        TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);
            actualApprovalReviewRequest.DeletionReason.Should().Be(ExpectedRetirementReason);

            // The audit values were stamped from a SYSTEM context, which is what makes
            // DeletedBy mean "nobody did this; it was answered".
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageApprovalReviewRequest,
                    It.Is<SecurityContext>(context => context.IsSystemIdentity),
                    ExpectedRetirementReason),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Removed),
                Times.Once);

            // No ProcessedEvents bookkeeping on this path: the verb has no event address and no
            // handler, so both rows would be written with no reader.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Withdrawn before the invited person got to it, or a redelivered fact retiring it a
        /// second time. Either way the row is already gone: nothing is written and no removal
        /// fact is published, so subscribers are not told something happened when nothing did.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnAlreadyWithdrawnApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            randomApprovalReviewRequest.IsDeleted = true;
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestWorkflowService
                    .RetireAnsweredApprovalReviewRequestAsync(
                        inputApprovalReviewRequestId,
                        TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<ApprovalReviewRequestEventOperation>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetireIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid invalidApprovalReviewRequestId = Guid.Empty;

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.Id),
                values: "Id is required");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> retireTask =
                this.approvalReviewRequestWorkflowService
                    .RetireAnsweredApprovalReviewRequestAsync(
                        invalidApprovalReviewRequestId,
                        TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retireTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRetireIfRequestDoesNotExistAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            ApprovalReviewRequest noApprovalReviewRequest = null;

            var notFoundApprovalReviewRequestException =
                new NotFoundApprovalReviewRequestException(
                    message: "Approval review request not found with id: " +
                        $"{someApprovalReviewRequestId}.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalReviewRequest);

            // when
            ValueTask<ApprovalReviewRequest> retireTask =
                this.approvalReviewRequestWorkflowService
                    .RetireAnsweredApprovalReviewRequestAsync(
                        someApprovalReviewRequestId,
                        TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retireTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);
        }

        /// <summary>
        /// The system-identity guard is unreachable through the public seam — that seam mints the
        /// context itself two methods up — so this exercises it through the ambient
        /// <c>CreateSystemAsync</c> stub returning a non-system context. What it protects against
        /// is a future second caller of the private do-work supplying its own envelope, which is
        /// exactly how the routes #295 removed from the sibling used to arrive.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetireIfTheContextIsNotTheSystemAsync()
        {
            // given: a caller-shaped context reaches the do-work instead of a system-minted one
            this.systemContextIsGenuine = false;
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "Retiring an answered approval review request is the approval "
                        + "workflow's own act; no user may perform it.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> retireTask =
                this.approvalReviewRequestWorkflowService
                    .RetireAnsweredApprovalReviewRequestAsync(
                        someApprovalReviewRequestId,
                        TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retireTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
