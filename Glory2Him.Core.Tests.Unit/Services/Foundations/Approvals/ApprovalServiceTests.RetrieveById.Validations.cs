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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidApprovalId = Guid.Empty;

            var invalidApprovalException = new InvalidApprovalException(
                message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.Approvals.Approval> retrieveApprovalByIdTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    invalidApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    retrieveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalNotFoundAndLogItAsync()
        {
            // given
            Guid someApprovalId = Guid.NewGuid();
            Approval nullApproval = null;

            var notFoundApprovalException =
                new NotFoundApprovalException(
                    message: $"Approval not found with id: {someApprovalId}.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullApproval);

            // when
            ValueTask<Approval> retrieveApprovalByIdTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    retrieveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfApprovalIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Approval storageApproval = CreateRandomApproval();
            storageApproval.IsDeleted = true;
            Guid approvalId = storageApproval.Id;

            var notFoundApprovalException =
                new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApproval);

            // when
            ValueTask<Approval> retrieveApprovalByIdTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    retrieveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Approval read denied. Approval {approvalId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
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
            Approval storageApproval = CreateRandomApproval();
            storageApproval.IsDeleted = false;
            Guid approvalId = storageApproval.Id;

            var notFoundApprovalException =
                new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApproval);

            // when
            ValueTask<Approval> retrieveApprovalByIdTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    retrieveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Approval read denied. Approval {approvalId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            Approval storageApproval = CreateRandomApproval();
            storageApproval.IsDeleted = false;
            Guid approvalId = storageApproval.Id;

            var notFoundApprovalException =
                new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Approval> retrieveApprovalByIdTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    retrieveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    approvalId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Approval read denied. Approval {approvalId} " +
                        $"is not publicly visible and user \"{randomActorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
