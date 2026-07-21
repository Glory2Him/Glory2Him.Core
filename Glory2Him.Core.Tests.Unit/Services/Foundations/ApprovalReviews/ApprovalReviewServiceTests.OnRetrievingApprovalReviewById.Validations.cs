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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingApprovalReviewByIdEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalReview>?> onRetrieveTask =
                this.approvalReviewService.OnRetrievingApprovalReviewByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingApprovalReviewByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((ApprovalReview?)null);

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> onRetrieveTask =
                this.approvalReviewService.OnRetrievingApprovalReviewByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualApprovalReviewValidationException.InnerException
                .Should().BeOfType<NotFoundApprovalReviewException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
