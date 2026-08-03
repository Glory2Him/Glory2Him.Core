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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalCommentId = Guid.Empty;

            var invalidApprovalCommentException = new InvalidApprovalCommentException(
                message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalCommentException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment>
                retrieveApprovalCommentByIdTask =
                    this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                        invalidApprovalCommentId,
                        TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    retrieveApprovalCommentByIdTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalCommentNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();
            ApprovalComment nullApprovalComment = null;

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {someApprovalCommentId}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    someApprovalCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullApprovalComment);

            // when
            ValueTask<ApprovalComment> retrieveApprovalCommentByIdTask =
                this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    someApprovalCommentId,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    retrieveApprovalCommentByIdTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    someApprovalCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalCommentIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = true;
            Guid approvalCommentId = storageApprovalComment.Id;

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            // when
            ValueTask<ApprovalComment> retrieveApprovalCommentByIdTask =
                this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    retrieveApprovalCommentByIdTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Approval comment read denied. Approval comment {approvalCommentId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = false;
            Guid approvalCommentId = storageApprovalComment.Id;

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            // when
            ValueTask<ApprovalComment> retrieveApprovalCommentByIdTask =
                this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    retrieveApprovalCommentByIdTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Approval comment read denied. Approval comment {approvalCommentId} is not " +
                        "publicly readable and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfUserIsNotOwnerAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = false;
            Guid approvalCommentId = storageApprovalComment.Id;

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ApprovalComment> retrieveApprovalCommentByIdTask =
                this.approvalCommentService.RetrieveApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    retrieveApprovalCommentByIdTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Approval comment read denied. Approval comment {approvalCommentId} " +
                        $"is not publicly readable and user \"{randomActorUserId}\" is neither the " +
                        "author nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
