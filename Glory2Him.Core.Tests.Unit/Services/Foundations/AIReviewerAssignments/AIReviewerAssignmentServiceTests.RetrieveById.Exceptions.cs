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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// The keyed read's failure taxonomy — there is no <c>RetrieveAll</c> on this service (the
    /// operation set was deliberately narrowed versus the ApprovalReviewRequest template), so this
    /// is the whole of the single-entity <c>TryCatch</c> overload's read-path coverage that Add,
    /// Modify and Remove do not already exercise.
    /// </summary>
    public partial class AIReviewerAssignmentServiceTests
    {
        /// <summary>
        /// A GENUINE cancellation — the caller's token — passes straight through rather than
        /// being categorized as a timeout. The distinction is the `when` clause on the first
        /// catch: a dependency that gave up looks identical to a caller who walked away unless
        /// the token is consulted, and reporting an abandoned request as a storage fault would
        /// page somebody over nothing.
        /// </summary>
        [Fact]
        public async Task ShouldRethrowOnRetrieveByIdIfCancellationIsRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    Guid.NewGuid(),
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeptions.Xeption expectedInnerException)
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
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
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
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
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
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
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
