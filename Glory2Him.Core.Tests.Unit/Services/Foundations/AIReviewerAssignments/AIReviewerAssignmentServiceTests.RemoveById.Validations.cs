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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid invalidAIReviewerAssignmentId = Guid.Empty;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.Id),
                values: "Id is required");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfDeletionReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();
            string tooLongDeletionReason = GetRandomStringWithLengthOf(501);

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.DeletionReason),
                values: "Text exceed max length of 500 characters");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    tooLongDeletionReason,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid someAIReviewerAssignmentId = Guid.NewGuid();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not authenticated.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not allowed to manage AI reviewer assignments.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRemoveByIdIfAssignmentDoesNotExistAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();
            AIReviewerAssignment noAIReviewerAssignment = null;

            var notFoundAIReviewerAssignmentException =
                new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found with id: " +
                        $"{someAIReviewerAssignmentId}.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> removeTask =
                this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
