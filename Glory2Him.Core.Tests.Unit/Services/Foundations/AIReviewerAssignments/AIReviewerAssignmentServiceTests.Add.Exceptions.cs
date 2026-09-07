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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<AIReviewerAssignment> addTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
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
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> addTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// The route <c>UX_AIReviewerAssignments_ApprovalId</c> actually travels. A second LIVE
        /// assignment on the same approval trips it, and a unique-INDEX violation arrives as a
        /// type that does not derive from <c>DuplicateKeyException</c> — so without its own
        /// clause it would fall through to the general handler and be mis-reported as "our code
        /// is broken" rather than as a uniqueness rule doing its job.
        /// </summary>
        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var expectedAIReviewerAssignmentDependencyValidationException =
                new AIReviewerAssignmentDependencyValidationException(
                    message: "AI reviewer assignment dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> addTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyValidationException>(
                    addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<AIReviewerAssignment> addTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            var serviceException = new Exception();

            var failedAIReviewerAssignmentServiceException =
                new FailedAIReviewerAssignmentServiceException(
                    message: "Failed AI reviewer assignment service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedAIReviewerAssignmentServiceException =
                new AIReviewerAssignmentServiceException(
                    message: "AI reviewer assignment service error occurred, contact support.",
                    innerException: failedAIReviewerAssignmentServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<AIReviewerAssignment> addTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentServiceException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentServiceException))),
                Times.Once);
        }
    }
}
