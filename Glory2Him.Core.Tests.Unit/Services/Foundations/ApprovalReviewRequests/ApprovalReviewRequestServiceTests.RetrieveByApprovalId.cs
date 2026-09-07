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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// The ROUND-KEYED read: the same §14.7 posture as its unkeyed twin, over a slice the storage
    /// layer narrows and materialises rather than one the caller composes onto a live queryable
    /// and executes synchronously.
    ///
    /// <para>These are also the only tests exercising the <c>IReadOnlyList</c> <c>TryCatch</c>
    /// overload, so the failure cases below are the whole of its coverage.</para>
    /// </summary>
    public partial class ApprovalReviewRequestServiceTests
    {
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveApprovalReviewRequestsByApprovalIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            Guid inputApprovalId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            // the filler randomises ApprovalId, so it is pinned here - otherwise an assertion
            // that the id reached the broker would pass on a read that ignored it
            ApprovalReviewRequest firstRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            firstRequest.ApprovalId = inputApprovalId;

            ApprovalReviewRequest secondRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            secondRequest.ApprovalId = inputApprovalId;

            var storageApprovalReviewRequests =
                new List<ApprovalReviewRequest> { firstRequest, secondRequest };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequests);

            // when
            IReadOnlyList<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService
                    .RetrieveApprovalReviewRequestsByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then: the tier sees the whole round's invitations
            actualApprovalReviewRequests.Should().BeEquivalentTo(storageApprovalReviewRequests);

            // and the ROUND was asked for, rather than the table being read and narrowed here
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The token has to reach the database call, which is the whole point of the narrow read:
        /// the shape it replaced threaded a token through every method in the chain and then
        /// executed the query without it.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnRetrieveByApprovalIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid inputApprovalId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>());

            // when
            await this.approvalReviewRequestService.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                inputApprovalId,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    inputApprovalId, inputCancellationToken),
                Times.Once);
        }

        /// <summary>
        /// The same posture the unkeyed collection read carries: a row the caller may not see
        /// drops out of the set rather than erroring, and an anonymous caller sees none at all.
        /// </summary>
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldReturnNoApprovalReviewRequestsByApprovalIdToAnAnonymousCallerAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid inputApprovalId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest storageApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            storageApprovalReviewRequest.ApprovalId = inputApprovalId;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>
                        {
                            storageApprovalReviewRequest
                        });

            // when
            IReadOnlyList<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService
                    .RetrieveApprovalReviewRequestsByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequests.Should().BeEmpty();
        }

        /// <summary>
        /// Outside the tier, a caller sees only the invitations they are a party to — the ones
        /// they raised and the ones addressed to them — and never anybody else's. Identical to the
        /// unkeyed read's rule because it IS the same filter, re-run over the narrowed set.
        /// </summary>
        [Fact]
        public async Task ShouldReturnOnlyOwnApprovalReviewRequestsByApprovalIdOutsideTheTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid inputApprovalId = Guid.NewGuid();
            string callerUserId = Guid.NewGuid().ToString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest raisedByCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset, userId: callerUserId).Create();

            raisedByCaller.ApprovalId = inputApprovalId;

            ApprovalReviewRequest addressedToCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            addressedToCaller.ApprovalId = inputApprovalId;
            addressedToCaller.RequestedUserId = callerUserId;

            ApprovalReviewRequest somebodyElses =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            somebodyElses.ApprovalId = inputApprovalId;

            ApprovalReviewRequest withdrawnButOwnedByCaller =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset, userId: callerUserId).Create();

            withdrawnButOwnedByCaller.ApprovalId = inputApprovalId;
            withdrawnButOwnedByCaller.IsDeleted = true;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>
                        {
                            raisedByCaller,
                            addressedToCaller,
                            somebodyElses,
                            withdrawnButOwnedByCaller
                        });

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(callerUserId);

            // when
            IReadOnlyList<ApprovalReviewRequest> actualApprovalReviewRequests =
                await this.approvalReviewRequestService
                    .RetrieveApprovalReviewRequestsByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then: both parties' rows, and the withdrawn one is gone even though it is theirs
            actualApprovalReviewRequests.Should().BeEquivalentTo(
                new[] { raisedByCaller, addressedToCaller });
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByApprovalIdIfOperationCanceledOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByApprovalIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
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
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByApprovalIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var serviceException = new Exception();

            var failedApprovalReviewRequestServiceException =
                new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedApprovalReviewRequestServiceException =
                new ApprovalReviewRequestServiceException(
                    message: "Approval review request service error occurred, contact support.",
                    innerException: failedApprovalReviewRequestServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewRequestService.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestServiceException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestServiceException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestServiceException))),
                Times.Once);
        }
    }
}
