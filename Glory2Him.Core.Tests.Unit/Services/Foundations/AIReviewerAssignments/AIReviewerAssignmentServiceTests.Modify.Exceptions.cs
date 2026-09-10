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
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifyIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var expectedAIReviewerAssignmentDependencyException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualAIReviewerAssignmentDependencyException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentDependencyException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnModifyIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAIReviewerAssignmentException =
                new TimeoutAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAIReviewerAssignmentDependencyException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: timeoutAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualAIReviewerAssignmentDependencyException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentDependencyException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnModifyIfCancellationRequestedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                modifyAIReviewerAssignmentTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnModifyIfSqlErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            SqlException sqlException = GetSqlException();

            var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                message: "Failed AI reviewer assignment storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedAIReviewerAssignmentDependencyException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: failedStorageAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualAIReviewerAssignmentDependencyException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentDependencyException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The route <c>UX_AIReviewerAssignments_ApprovalId</c> travels even on a modify — this
        /// path routes through <c>UpdateAIReviewerAssignmentAsync</c>, so the same taxonomy
        /// applies, plus the concurrency conflict a write-then-read race can raise.
        /// </summary>
        [Theory]
        [MemberData(nameof(ModifyDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfErrorOccursAndLogItAsync(
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
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(thrownException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyValidationException actualAIReviewerAssignmentDependencyValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentDependencyValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentDependencyValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnModifyIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            var serviceException = new Exception();

            var failedAIReviewerAssignmentServiceException = new FailedAIReviewerAssignmentServiceException(
                message: "Failed AI reviewer assignment service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedAIReviewerAssignmentServiceException = new AIReviewerAssignmentServiceException(
                message: "AI reviewer assignment service error occurred, contact support.",
                innerException: failedAIReviewerAssignmentServiceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentServiceException actualAIReviewerAssignmentServiceException =
                await Assert.ThrowsAsync<AIReviewerAssignmentServiceException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentServiceException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentServiceException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(someAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
