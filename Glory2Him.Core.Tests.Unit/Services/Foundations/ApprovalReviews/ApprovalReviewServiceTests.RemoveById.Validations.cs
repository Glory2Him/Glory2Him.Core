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

using Glory2Him.Core.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalReviewId = Guid.Empty;

            var invalidApprovalReviewException = new InvalidApprovalReviewException(
                message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.UpsertDataList(
                key: nameof(ApprovalReview.Id),
                value: "Id is required");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalReviewException);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    invalidApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfDeletionReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();
            string invalidDeletionReason = GetRandomStringWithLengthOf(501);

            var invalidApprovalReviewException = new InvalidApprovalReviewException(
                message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.UpsertDataList(
                key: nameof(ApprovalReview.DeletionReason),
                value: $"Text exceed max length of {invalidDeletionReason.Length - 1} characters");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalReviewException);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    deletionReason: invalidDeletionReason,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfApprovalReviewNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalReviewId = Guid.NewGuid();
            ApprovalReview noApprovalReview = null;

            var notFoundApprovalReviewException = new NotFoundApprovalReviewException(
                message: $"Approval review not found with id: {someApprovalReviewId}.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalReview);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someApprovalReviewId = Guid.NewGuid();

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not authenticated.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);
            Guid someApprovalReviewId = Guid.NewGuid();

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is blocked from contributing approval reviews.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotOwnerAndNotAdminAndLogItAsync()
        {
            // given: a peer reviewer cannot withdraw someone else's verdict
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomActorUserId = GetRandomString();
            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.IsDeleted = false;
            Guid someApprovalReviewId = storageApprovalReview.Id;

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not allowed to remove this approval review.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsAdminButNotOwnerAndLogItAsync()
        {
            // given: the administrator escape is closed. This used to assert the opposite — that an
            // administrator could retract anyone's review — and that is withdrawn (§14.7 rule 5). An
            // administrator who needs past a review bypasses the block rather than deleting it.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomActorUserId = GetRandomString();
            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.IsDeleted = false;
            Guid someApprovalReviewId = storageApprovalReview.Id;

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not allowed to remove this approval review.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAllowedEvenWhenAlreadyDeletedAndLogItAsync()
        {
            // given: permission is answered before the idempotent already-deleted
            // short-circuit, so an unauthorized caller learns nothing about the row
            string randomActorUserId = GetRandomString();
            ApprovalReview alreadyDeletedApprovalReview = CreateRandomApprovalReview();
            alreadyDeletedApprovalReview.IsDeleted = true;
            Guid someApprovalReviewId = alreadyDeletedApprovalReview.Id;

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not allowed to remove this approval review.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualApprovalReviewValidationException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualApprovalReviewValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfTheRoundHasClosedAndLogItAsync()
        {
            // given: the owner withdrawing their own verdict after the round closed. Removal is
            // an amendment by deletion, so it takes the same §7.7 rule 2b window as modify —
            // otherwise an entity could sit Approved with the rejection that blocked it quietly
            // gone, and nothing would re-run the workflow.
            string ownerUserId = GetRandomString();
            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.IsDeleted = false;
            storageApprovalReview.CreatedBy = ownerUserId;
            Guid someApprovalReviewId = storageApprovalReview.Id;

            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalNotOpenForReview);

            var unauthorizedApprovalReviewException = new UnauthorizedApprovalReviewException(
                message: "The current user is not allowed to review approvals.");

            var expectedApprovalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalReviewException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedApprovalReviewValidationException);

            // the ApprovalId comes from the stored row, and the call is shaped as an amendment
            this.accessBrokerMock.Verify(broker =>
                broker.MayRecordApprovalReviewAsync(
                    storageApprovalReview.ApprovalId,
                    true,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            // the soft delete never lands, and nothing is announced
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<ApprovalReviewEventOperation>()),
                        Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveIfTheReviewIsDismissedAndLogItAsync()
        {
            // given: a dismissed review is closed and may not be touched — withdrawal included.
            // §9.5 retains it as evidence that a verdict once applied to superseded content, so
            // soft-deleting one destroys the record the dismissal exists to keep. Modify already
            // refused this; without the same guard here, deleting was the shorter route to the
            // same outcome.
            string randomUserId = GetRandomString();
            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.CreatedBy = randomUserId;
            storageApprovalReview.IsDeleted = false;
            storageApprovalReview.StatusId = ApprovalStatus.Dismissed;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "A dismissed approval review cannot be amended. " +
                        "Submit a new review instead.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            // when
            ValueTask<ApprovalReview> removeApprovalReviewByIdTask =
                this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    removeApprovalReviewByIdTask.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedApprovalReviewValidationException);

            // nothing written — the audit record stands — and nothing announced
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()),
                        Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<ApprovalReviewEventOperation>()),
                        Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewValidationException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
