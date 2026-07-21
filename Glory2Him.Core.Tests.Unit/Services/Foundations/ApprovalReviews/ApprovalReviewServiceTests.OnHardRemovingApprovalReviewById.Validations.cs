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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalReviewByIdEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalReview>?> onHardRemovingTask =
                this.approvalReviewService.OnHardRemovingApprovalReviewByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onHardRemovingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalReviewByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalReviewException = new InvalidApprovalReviewException(
                message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.UpsertDataList(
                key: nameof(ApprovalReview.Id),
                value: "Id is required");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onHardRemovingTask =
                this.approvalReviewService.OnHardRemovingApprovalReviewByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

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
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalReviewByIdEventWhenApprovalReviewNotFoundAsync()
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();
            ApprovalReview noApprovalReview = null!;

            var requestEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = someApprovalReviewId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundApprovalReviewException = new NotFoundApprovalReviewException(
                message: $"Approval review not found with id: {someApprovalReviewId}.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalReview);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onHardRemovingTask =
                this.approvalReviewService.OnHardRemovingApprovalReviewByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
