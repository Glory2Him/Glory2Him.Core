// ─────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ─────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(AssociationDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a ROUTINE refusal raised by the foundation - the endpoint veto, the
            // owner test, the Publishers tier, a terminal row, a uniqueness collision -
            // leaves as a dependency VALIDATION failure carrying the foundation's own
            // inner. It must NOT leave as a dependency error: that is the 424-for-a-400
            // shape the audit on ApprovalOrchestrationService found.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AssociationDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRemoveIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a genuine downstream failure stays a dependency failure, and is
            // never re-surfaced as the foundation's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything outside the recognised families leaves as a service
            // exception, wrapping FailedAssociationOrchestrationServiceException.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var serviceException = new Exception("Service error occurred.");

            var failedAssociationOrchestrationServiceException =
                new FailedAssociationOrchestrationServiceException(
                    message: "Failed content item association orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new AssociationOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, " +
                        "contact support.",
                    innerException: failedAssociationOrchestrationServiceException);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRemoveIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose OWN token was not cancelled is a
            // dependency timeout rather than a caller cancellation, and becomes a timeout
            // dependency error.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationOrchestrationException =
                new TimeoutAssociationOrchestrationException(
                    message: "Failed content item association orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: timeoutAssociationOrchestrationException);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveIfCancellationRequestedAsync()
        {
            // given: a token already cancelled throws BEFORE any dependency call, and is
            // rethrown rather than wrapped - a caller cancellation is never masked as a
            // timeout.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(removeTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
