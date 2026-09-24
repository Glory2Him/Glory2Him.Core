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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // THE DELEGATED CALL FAILING. The foundation's own dependency and service failures keep
        // the category they have on the method path, carrying the foundation's inner exception
        // rather than the foundation's type (§ARC12.2).
        [Theory]
        [MemberData(nameof(AssociationDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingIfTheFoundationHandlerFailsAndLogItAsync(
            Xeption foundationException)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The foundation handler's own refusals stay validation-shaped through this layer, exactly
        // as its method-path refusals do.
        [Theory]
        [MemberData(nameof(AssociationDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingIfTheFoundationHandlerRefusesAndLogItAsync(
            Xeption foundationException)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // AN ENDPOINT READ THAT FAILED IS NEVER TREATED AS RESOLVED. A store that could not answer
        // is a dependency failure, not a not-found, and above all not a pass: the foundation is
        // never handed the envelope.
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingIfAnEndpointReadFailsAndNotFallOpenAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            var innerException = new Xeption(message: GetRandomString());

            var contentItemDependencyException =
                new ContentItemDependencyException(
                    message: GetRandomString(),
                    innerException: innerException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: innerException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    addRequest.EntityAKeyId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemDependencyException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A TIMEOUT — an OperationCanceledException whose token was NOT cancelled — is a
        // dependency failure, never the caller's cancellation.
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingIfADependencyTimesOutAndLogItAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
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
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            var serviceException = new Exception(GetRandomString());

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

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A caller who has already cancelled pays for nothing — not the HMAC, not a read — and
        // the cancellation propagates as itself, never masked as a timeout or wrapped, exactly as
        // the method path's add does.
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingIfCancellationRequestedAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(onAddingTask.AsTask);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE EARLY-DEDUPE QUESTION FAILING (#631 criterion 9). A store that could not say whether
        // the event was applied is a dependency failure — not "no", which would re-apply it, and
        // not "yes", which would drop it.
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingIfTheDuplicateQuestionFailsAndLogItAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            var innerException = new Xeption(message: GetRandomString());

            var associationDependencyException =
                new AssociationDependencyException(
                    message: GetRandomString(),
                    innerException: innerException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: innerException);

            this.associationServiceMock.Setup(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(associationDependencyException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<string> OccupancyProbes() =>
            new TheoryData<string>
            {
                "the pair probe",
                "the overlap probe",
            };

        // A PROBE THAT FAILED IS NEVER TREATED AS FINDING THE PAIR UNOCCUPIED (#631 criterion 9).
        // Falling open here is exactly the takedown-laundering insert the probes exist to stop.
        [Theory]
        [MemberData(nameof(OccupancyProbes))]
        public async Task ShouldThrowDependencyExceptionOnAddingIfAnOccupancyProbeFailsAndNotFallOpenAsync(
            string failingProbe)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            var innerException = new Xeption(message: GetRandomString());

            var associationDependencyException =
                new AssociationDependencyException(
                    message: GetRandomString(),
                    innerException: innerException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: innerException);

            if (failingProbe == "the pair probe")
            {
                this.associationServiceMock.Setup(service =>
                    service.FindAssociationByPairAsync(
                        It.IsAny<Association>(),
                        inputEnvelope,
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(associationDependencyException);
            }
            else
            {
                this.associationServiceMock.Setup(service =>
                    service.FindOverlappingAssociationAsync(
                        It.IsAny<Association>(),
                        inputEnvelope,
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(associationDependencyException);
            }

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
