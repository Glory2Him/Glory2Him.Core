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
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// The remove path writes through <c>Update</c>, so its dependency-validation surface is
        /// the concurrency clash rather than the uniqueness clash the add path meets: two
        /// moderators removing the same assignment at once is a realistic race, and it must
        /// read as "try again" rather than as a broken service.
        /// </summary>
        [Theory]
        [MemberData(nameof(RemoveDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();

            var expectedAIReviewerAssignmentDependencyValidationException =
                new AIReviewerAssignmentDependencyValidationException(
                    message: "AI reviewer assignment dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();
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
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentServiceException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentServiceException))),
                Times.Once);
        }
    }
}
