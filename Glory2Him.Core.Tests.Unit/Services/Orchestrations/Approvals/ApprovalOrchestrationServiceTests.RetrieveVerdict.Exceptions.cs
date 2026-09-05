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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // The wrapper messages below are asserted verbatim because they are the contract the
        // exposer surfaces. They still read "content item association" — the partial was seeded
        // from the Association orchestration and the strings were never re-worded. The tests
        // pin what the service ACTUALLY says, so the day someone corrects the wording they are
        // told, rather than the correction landing silently on a caller matching on it.
        private const string ExpectedDependencyMessage =
            "Content item association orchestration dependency error occurred, contact support.";

        private const string ExpectedDependencyValidationMessage =
            "Content item association orchestration dependency validation error occurred, " +
                "fix the errors and try again.";

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveVerdictIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a validation-shaped failure from the Approval foundation is a fault in what
            // this orchestration asked for, not in what the caller asked for — so it becomes a
            // DEPENDENCY validation exception, carrying the foundation exception's OWN inner and
            // never the foundation type itself (§1.1.3 — no foundation exception leaks upward).
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // §14.5 rule 3 is asked FIRST on this read, ahead of the approval lookup, so the
            // visibility probe is the one access-broker call every verdict makes.
            this.accessBrokerMock.Verify(broker =>
                broker.IsEntityVisibleAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Nothing was asked of the POLICY side — a verdict half-composed from a failed
            // read is worse than none.
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveVerdictIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the foundation's dependency and service failures are both external faults,
            // so they collapse to one orchestration dependency category. Logged with LogError —
            // not LogCritical — which is what keeps this consistent with every other wrapper on
            // the service and is asserted here so a later "upgrade" to critical is noticed.
            EntityType entityType = EntityType.Tag;
            Guid entityId = Guid.NewGuid();

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            // §14.5 rule 3 is asked FIRST on this read, ahead of the approval lookup, so the
            // visibility probe is the one access-broker call every verdict makes.
            this.accessBrokerMock.Verify(broker =>
                broker.IsEntityVisibleAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveVerdictIfTheAccessBrokerDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the mapping is a property of the TryCatch, not of which dependency tripped
            // it. The verdict reaches storage through two very different collaborators — the
            // Approval foundation and the policy broker — and a caller cannot be asked to care
            // which one failed, so the same families must land in the same category from both.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch());

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // Conditions come before the two decision questions, so a failure there must stop
            // the composition rather than fall through to a verdict with no blocks in it.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveVerdictIfTheAccessBrokerDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the failure is raised by the FIRST decision question (the plain approve,
            // isBypassRequested false). The bypass probe that follows must never be asked once
            // the plain one has failed — a second policy call on a broker already known to be
            // failing buys nothing and would log twice.
            EntityType entityType = EntityType.Comment;
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch());
            SetupConditions(CreateMetConditions());

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    false,
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    true,
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamXeptionOnRetrieveVerdictAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: a Xeption from a collaborator this service does not name a catch for still
            // has to be categorized — the broad downstream catch calls it a dependency issue and
            // re-surfaces the failing exception's OWN inner, so no foreign exception type ever
            // crosses the layer boundary (§1.1.3).
            EntityType entityType = EntityType.BibleReference;
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch());

            var innerException = new Xeption(message: GetRandomString());

            var downstreamException = new Xeption(
                message: GetRandomString(),
                innerException: innerException);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: innerException);

            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(downstreamException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveVerdictIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose OWN token was never cancelled did not come from
            // the caller — it is a dependency that gave up, i.e. a timeout. The distinction is
            // drawn by the exception filter alone, and getting it backwards would report every
            // deliberate cancellation as a support-worthy failure while silently swallowing real
            // timeouts. That is precisely the pair this test and the next one exist to separate.
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            // The default constructor leaves CancellationToken at None, whose
            // IsCancellationRequested is false — the timeout half of the filter.
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalOrchestrationException =
                new TimeoutApprovalOrchestrationException(
                    message: "Failed content item association orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            // The timeout wrapper is kept whole rather than unwrapped, so the call site can still
            // read "this was a timeout" off the inner exception.
            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: timeoutApprovalOrchestrationException);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnRetrieveVerdictIfItsTokenWasCancelledAsync()
        {
            // given: the mirror of the test above. This OperationCanceled carries a token that
            // WAS cancelled, so it is the caller's own withdrawal travelling back up — it must
            // arrive untouched. Wrapping it would turn a user navigating away into an error the
            // support channel is asked to explain, and logging it would fill the log with noise
            // nobody can act on.
            EntityType entityType = EntityType.Attachment;
            Guid entityId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can
            // decide the branch — the entry guard cannot short-circuit this run.
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualException =
                await Assert.ThrowsAsync<OperationCanceledException>(retrieveTask.AsTask);

            // then: the very same instance, not an equivalent one — a rethrow, never a re-wrap.
            actualException.Should().BeSameAs(operationCanceledException);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveVerdictIfCancellationRequestedAsync()
        {
            // given: a token already cancelled when the call is made. The guard sits ahead of
            // the validations and the envelope, so an abandoned request costs no storage read
            // and no policy evaluation.
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveVerdictIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything the service did not anticipate is its own fault until proven
            // otherwise — categorized as a SERVICE error rather than a dependency one, so a bug
            // in this orchestration is never filed against the collaborator it happened next to.
            EntityType entityType = EntityType.Association;
            Guid entityId = Guid.NewGuid();
            var serviceException = new Exception("Service error occurred.");

            var failedApprovalOrchestrationServiceException =
                new FailedApprovalOrchestrationServiceException(
                    message: "Failed content item association orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new ApprovalOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, contact support.",
                    innerException: failedApprovalOrchestrationServiceException);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalVerdict> retrieveTask =
                this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationServiceException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            // §14.5 rule 3 is asked FIRST on this read, ahead of the approval lookup, so the
            // visibility probe is the one access-broker call every verdict makes.
            this.accessBrokerMock.Verify(broker =>
                broker.IsEntityVisibleAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
