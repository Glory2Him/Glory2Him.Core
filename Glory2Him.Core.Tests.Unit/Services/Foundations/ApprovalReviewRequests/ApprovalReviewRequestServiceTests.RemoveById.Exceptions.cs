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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// The withdraw path writes through <c>Update</c>, so its dependency-validation surface is
        /// the concurrency clash rather than the uniqueness clash the add path meets: two
        /// moderators withdrawing the same invitation at once is a realistic race, and it must
        /// read as "try again" rather than as a broken service.
        /// </summary>
        [Theory]
        [MemberData(nameof(RemoveDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var expectedApprovalReviewRequestDependencyValidationException =
                new ApprovalReviewRequestDependencyValidationException(
                    message: "Approval review request dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalReviewRequestException =
                new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: failedStorageApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedApprovalReviewRequestServiceException =
                new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedApprovalReviewRequestServiceException =
                new ApprovalReviewRequestServiceException(
                    message: "Approval review request service error occurred, contact support.",
                    innerException: failedApprovalReviewRequestServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalReviewRequest> removeTask =
                this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalReviewRequestServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestServiceException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestServiceException))),
                Times.Once);
        }
    }
}
