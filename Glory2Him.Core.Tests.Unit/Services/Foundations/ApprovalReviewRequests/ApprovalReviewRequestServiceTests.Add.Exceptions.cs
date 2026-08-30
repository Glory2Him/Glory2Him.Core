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
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException = new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalReviewRequestException =
                new TimeoutApprovalReviewRequestException(
                    message: "Failed approval review request timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: timeoutApprovalReviewRequestException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalReviewRequest> addTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var expectedApprovalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReviewRequest> addTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// The route §7.9 rule 1 actually travels. A second ACTIVE invitation for the same person
        /// on the same approval trips
        /// <c>UX_ApprovalReviewRequests_ApprovalId_RequestedUserId</c>, and a unique-INDEX
        /// violation arrives as a type that does not derive from <c>DuplicateKeyException</c> —
        /// so without its own clause it would fall through to the general handler and be
        /// mis-reported as "our code is broken" rather than as a uniqueness rule doing its job.
        /// </summary>
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var expectedApprovalReviewRequestDependencyValidationException =
                new ApprovalReviewRequestDependencyValidationException(
                    message: "Approval review request dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ApprovalReviewRequest> addTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyValidationException>(
                    addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalReviewRequest> addTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalReviewRequest> addTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestServiceException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestServiceException))),
                Times.Once);
        }
    }
}
