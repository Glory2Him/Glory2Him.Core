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
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            Guid invalidAIReviewerAssignmentId = Guid.Empty;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.Id),
                values: "Id is required");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignmentId,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

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
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfAssignmentDoesNotExistAndLogItAsync()
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
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        /// <summary>
        /// A removed assignment reads as absent rather than as forbidden, mirroring
        /// ApprovalReviewRequest's withdrawn-invitation posture.
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfAssignmentIsRemovedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            randomAIReviewerAssignment.IsDeleted = true;
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;

            var notFoundAIReviewerAssignmentException =
                new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found with id: " +
                        $"{inputAIReviewerAssignmentId}.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.Is<string>(message =>
                    message.Contains("removed")
                        && message.Contains(inputAIReviewerAssignmentId.ToString()))),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfCallerIsAnonymousAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given: who Berean has been assigned to is never public
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;

            var notFoundAIReviewerAssignmentException =
                new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found with id: " +
                        $"{inputAIReviewerAssignmentId}.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        /// <summary>
        /// Signed in, but not in a review role. It reads as not-found rather than unauthorized —
        /// the §14.6 rule 12 denial posture.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveByIdIfCallerHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;

            var notFoundAIReviewerAssignmentException =
                new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found with id: " +
                        $"{inputAIReviewerAssignmentId}.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> retrieveTask =
                this.aiReviewerAssignmentService.RetrieveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(It.Is<string>(message =>
                    message.Contains("not in a review role"))),
                Times.Once);
        }
    }
}
