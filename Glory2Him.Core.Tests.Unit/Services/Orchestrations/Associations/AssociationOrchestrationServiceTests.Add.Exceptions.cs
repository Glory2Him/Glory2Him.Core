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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(AssociationDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: an Association foundation dependency-validation failure surfaces as an
            // orchestration dependency-validation exception, carrying the foundation's OWN inner
            // (never the foundation exception type itself — §1.1.3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AssociationDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamEndpointDependencyFailureAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: an endpoint service's dependency failure (not a not-found) is categorized as
            // an orchestration dependency error via the broad downstream catch, carrying the
            // endpoint exception's OWN inner — never re-surfaced as the endpoint's type (§1.1.3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();

            var innerException = new Xeption();

            var contentItemDependencyException =
                new ContentItemDependencyException(
                    message: "downstream failure",
                    innerException: innerException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: innerException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemDependencyException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose token was NOT cancelled is a dependency timeout,
            // not a caller cancellation — it is turned into a timeout dependency error.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

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
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: timeoutAssociationOrchestrationException);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddIfCancellationRequestedAsync()
        {
            // given: a genuine caller cancellation must propagate as-is, never be masked as a
            // timeout or wrapped in an orchestration exception.
            Association rawRequest = CreateRawAddRequest();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(addTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Association rawRequest = CreateRawAddRequest();
            var serviceException = new Exception("Service error occurred.");

            var failedAssociationOrchestrationServiceException =
                new FailedAssociationOrchestrationServiceException(
                    message: "Failed content item association orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new AssociationOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, contact support.",
                    innerException: failedAssociationOrchestrationServiceException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
