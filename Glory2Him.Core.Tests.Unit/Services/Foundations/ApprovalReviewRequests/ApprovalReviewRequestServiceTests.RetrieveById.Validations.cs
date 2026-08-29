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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
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
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    invalidApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retrieveTask.AsTask);

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
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfRequestDoesNotExistAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
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
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);
        }

        /// <summary>
        /// A withdrawn invitation reads as absent rather than as forbidden — the row is gone as
        /// far as any caller is concerned, and saying "withdrawn" would confirm that somebody was
        /// once invited, which is precisely the coordination detail §16.7.4 keeps inside the tier.
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfRequestIsWithdrawnAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            randomApprovalReviewRequest.IsDeleted = true;
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;

            var notFoundApprovalReviewRequestException =
                new NotFoundApprovalReviewRequestException(
                    message: "Approval review request not found with id: " +
                        $"{inputApprovalReviewRequestId}.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequest);

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            // §14.5: the TRUE reason is written server-side while the caller is told only
            // not-found. Asserted because the two halves are what make the posture a posture —
            // dropping the log would leave an operator unable to explain a support call, and
            // nothing else in the suite would notice.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.Is<string>(message =>
                    message.Contains("withdrawn")
                        && message.Contains(inputApprovalReviewRequestId.ToString()))),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfCallerIsAnonymousAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given: who has been asked to review is never public
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;

            var notFoundApprovalReviewRequestException =
                new NotFoundApprovalReviewRequestException(
                    message: "Approval review request not found with id: " +
                        $"{inputApprovalReviewRequestId}.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequest);

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);
        }

        /// <summary>
        /// Signed in, but neither a party to the invitation nor inside the review tier. It reads
        /// as not-found rather than unauthorized, which is the §14.5 denial posture: a refusal
        /// must not become a probe for who is being asked to review what.
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfCallerIsAnOutsiderAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;

            var notFoundApprovalReviewRequestException =
                new NotFoundApprovalReviewRequestException(
                    message: "Approval review request not found with id: " +
                        $"{inputApprovalReviewRequestId}.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync("somebody-with-no-part-in-this-round");

            // when
            ValueTask<ApprovalReviewRequest> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            // §14.5 again: the caller sees not-found, the log names the actor and why.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(It.Is<string>(message =>
                    message.Contains("somebody-with-no-part-in-this-round")
                        && message.Contains("neither a party to it nor in a review role"))),
                Times.Once);
        }
    }
}
