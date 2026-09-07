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
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// The ROUND-KEYED read, singular unlike ApprovalReviewRequest's — at most one assignment is
    /// ever live per approval, so there is no list to narrow, only the row or its absence. These
    /// are also the only tests exercising <c>TryCatchNullable</c>.
    /// </summary>
    public partial class AIReviewerAssignmentServiceTests
    {
        /// <summary>
        /// A bad round id is the CALLER's fault and must be reported as one, not folded into the
        /// ordinary "no assignment" null answer.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByApprovalIdIfApprovalIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);

            // when
            ValueTask<AIReviewerAssignment?> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidAIReviewerAssignmentException>();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAIReviewerAssignmentByApprovalIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            Guid inputApprovalId = Guid.NewGuid();

            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            randomAIReviewerAssignment.ApprovalId = inputApprovalId;
            AIReviewerAssignment expectedAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerAssignment);

            // when
            AIReviewerAssignment? actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService
                    .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldReturnNullOnRetrieveByApprovalIdWhenNoAssignmentExistsAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            Guid inputApprovalId = Guid.NewGuid();
            AIReviewerAssignment noAIReviewerAssignment = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            AIReviewerAssignment? actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService
                    .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeNull();
        }

        /// <summary>
        /// Denied the SAME WAY a missing assignment is answered: null. There is no separate
        /// not-found shape for a round-keyed "or null" read to diverge into (§14.6 rule 12).
        /// </summary>
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldReturnNullOnRetrieveByApprovalIdWhenCallerIsAnonymousAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid inputApprovalId = Guid.NewGuid();

            AIReviewerAssignment storedAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            storedAIReviewerAssignment.ApprovalId = inputApprovalId;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAIReviewerAssignment);

            // when
            AIReviewerAssignment? actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService
                    .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldReturnNullOnRetrieveByApprovalIdWhenCallerHasNoReviewRoleAsync(
            string[] nonReviewRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            Guid inputApprovalId = Guid.NewGuid();

            AIReviewerAssignment storedAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            storedAIReviewerAssignment.ApprovalId = inputApprovalId;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAIReviewerAssignment);

            // when
            AIReviewerAssignment? actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService
                    .RetrieveAIReviewerAssignmentByApprovalIdAsync(
                        inputApprovalId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The token has to reach the database call, which is the whole point of the narrow read.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnRetrieveByApprovalIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid inputApprovalId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;
            AIReviewerAssignment noAIReviewerAssignment = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            await this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                inputApprovalId,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    inputApprovalId, inputCancellationToken),
                Times.Once);
        }

        /// <summary>
        /// A GENUINE cancellation passes straight through rather than being categorized as a
        /// timeout — see the sibling RetrieveById test for the full rationale.
        /// </summary>
        [Fact]
        public async Task ShouldRethrowOnRetrieveByApprovalIdIfCancellationIsRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AIReviewerAssignment?> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    Guid.NewGuid(),
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByApprovalIdIfOperationCanceledOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var operationCanceledException = new OperationCanceledException();
            var timeoutException = new TimeoutException("The dependency operation timed out.");

            var timeoutAIReviewerAssignmentException =
                new TimeoutAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: timeoutAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<AIReviewerAssignment?> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveByApprovalIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            SqlException sqlException = GetSqlException();

            var failedStorageAIReviewerAssignmentException =
                new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: failedStorageAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<AIReviewerAssignment?> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByApprovalIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var serviceException = new Exception();

            var failedAIReviewerAssignmentServiceException =
                new FailedAIReviewerAssignmentServiceException(
                    message: "Failed AI reviewer assignment service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedAIReviewerAssignmentServiceException =
                new AIReviewerAssignmentServiceException(
                    message: "AI reviewer assignment service error occurred, contact support.",
                    innerException: failedAIReviewerAssignmentServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<AIReviewerAssignment?> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentServiceException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentServiceException))),
                Times.Once);
        }
    }
}
