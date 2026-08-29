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
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
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
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    invalidApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    removeTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfDeletionReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            string tooLongDeletionReason = GetRandomStringWithLengthOf(501);

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.DeletionReason),
                values: "Text exceed max length of 500 characters");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    tooLongDeletionReason,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    removeTask.AsTask);

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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not authenticated.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    removeTask.AsTask);

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

        /// <summary>
        /// Withdrawal is wide across the tier but stops AT the tier: §7.9 rule 5 opens it to
        /// everyone above the read-only view, not to everyone. A signed-in reader with no part in
        /// the round cannot cancel somebody else's invitation.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not allowed to request approval reviews.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    removeTask.AsTask);

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
        public async Task ShouldThrowNotFoundExceptionOnRemoveByIdIfRequestDoesNotExistAndLogItAsync()
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
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
