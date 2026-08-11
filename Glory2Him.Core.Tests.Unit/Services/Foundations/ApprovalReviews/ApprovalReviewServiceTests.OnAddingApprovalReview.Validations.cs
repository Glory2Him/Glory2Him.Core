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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalReviewEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalReview>? nullEnvelope = null;

            var invalidApprovalReviewEventException =
                new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onAddingTask =
                this.approvalReviewService.OnAddingApprovalReviewAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalReviewEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidApprovalReviewEventException =
                new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onAddingTask =
                this.approvalReviewService.OnAddingApprovalReviewAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalReviewEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalReviewEventException =
                new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onAddingTask =
                this.approvalReviewService.OnAddingApprovalReviewAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalReviewEventWhenIntegrityVerificationFailsAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<ApprovalReview>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ApprovalReview { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(ApprovalReview)}{ApprovalReviewEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidApprovalReviewEventException =
                new InvalidApprovalReviewEventException(
                    message: "Invalid approval review event. Integrity verification failed.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onAddingTask =
                this.approvalReviewService.OnAddingApprovalReviewAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
