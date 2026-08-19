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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // The decision touches FOUR dependency calls — the probe, the one authorisation, the
        // storage read and the write — and they are asserted separately rather than as one
        // representative case. The catch chain is shared with the verdict read (one generic
        // TryCatch), so the risk is not that a family is mapped wrongly but that a call site is
        // reached OUTSIDE the chain, or that a failure part-way through leaves the entity sync
        // command already published. Only per-dependency tests can tell those apart.

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnDecideIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a validation-shaped failure from the Approval foundation is a fault in what
            // this orchestration asked for, not in what the caller asked for, so it becomes a
            // DEPENDENCY validation exception carrying the foundation exception's OWN inner and
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
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // The whole chain funnels through LogError — no family on this service escalates to
            // LogCritical — so the day one is "upgraded" a test says so rather than a log filter.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // The probe is the first call that can fail. Nothing may have been asked of the
            // policy broker and nothing may have been written.
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnDecideIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the foundation's dependency and service failures are both external faults,
            // so they collapse to one orchestration dependency category.
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
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnDecideIfTheAccessBrokerDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the mapping is a property of the TryCatch, not of which dependency tripped
            // it — the same families must land in the same category whether the Approval
            // foundation or the policy broker raised them.
            EntityType entityType = EntityType.Link;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // An authorisation that could not be obtained is not an authorisation. The row must
            // be left exactly as it was found.
            VerifyNoDecideExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnDecideIfTheAccessBrokerDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the ONE authorisation failed outright. §16.7.1 puts the question here and
            // nowhere else, so a failure to ask it can only mean the decision does not proceed.
            EntityType entityType = EntityType.Comment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnDecideIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the probe returns a projection, so the row itself is read a second time
            // before it can be written. That read is a THIRD chance to fail, after the caller has
            // already been authorised — and it must still be caught by the same chain rather than
            // escaping as a raw foundation exception.
            EntityType entityType = EntityType.Reaction;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnDecideIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the read of the row that is about to be decided failed.
            EntityType entityType = EntityType.BibleReference;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnDecideIfTheWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the write is the LAST thing that can fail before the entity sync command is
            // published, and it is the failure that matters most. §9.8 makes the Approval row the
            // source of truth; a command published for a decision that was never stored would
            // drive the entity to a state no approval records.
            EntityType entityType = EntityType.Association;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            SetupDecideExceptionsStorageRead(
                CreateDecideExceptionsApproval(approvalId, entityType, entityId));

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsCommandPublished();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnDecideIfTheWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same guarantee for the external-fault families — a store that was
            // unreachable decided nothing, so nothing may be announced.
            EntityType entityType = EntityType.Link;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            SetupDecideExceptionsStorageRead(
                CreateDecideExceptionsApproval(approvalId, entityType, entityId));

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsCommandPublished();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamXeptionOnDecideAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: a Xeption from a collaborator this service names no catch for still has to
            // be categorized — the broad downstream catch calls it a dependency issue and
            // re-surfaces the failing exception's OWN inner, so no foreign exception type ever
            // crosses the layer boundary (§1.1.3).
            EntityType entityType = EntityType.Attachment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            var innerException = new Xeption(message: GetRandomString());

            var downstreamException = new Xeption(
                message: GetRandomString(),
                innerException: innerException);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: innerException);

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(downstreamException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnDecideIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything the service did not anticipate is its own fault until proven
            // otherwise — categorized as a SERVICE error rather than a dependency one, so a bug
            // in this orchestration is never filed against the collaborator it happened next to.
            // The raw Exception is thrown from the WRITE, the deepest call, to prove the broad
            // catch still sits outside the whole flow and not merely around its first step.
            EntityType entityType = EntityType.ContentItem;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            SetupDecideExceptionsStorageRead(
                CreateDecideExceptionsApproval(approvalId, entityType, entityId));

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
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationServiceException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            // A service-category failure is the one place an escalation to LogCritical would look
            // reasonable. It is not what the chain does, and the difference is pinned here.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsCommandPublished();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnDecideIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose OWN token was never cancelled did not come from
            // the caller — it is a dependency that gave up, i.e. a timeout. The distinction is
            // drawn by the exception filter alone, and getting it backwards would report every
            // deliberate cancellation as a support-worthy failure while silently swallowing real
            // timeouts. That is precisely the pair this test and the next one exist to separate,
            // and it is asserted from the WRITE — where a cancellation is most consequential,
            // because a decision may or may not have landed.
            EntityType entityType = EntityType.Tag;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            SetupDecideExceptionsStorageRead(
                CreateDecideExceptionsApproval(approvalId, entityType, entityId));

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
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    decideTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoDecideExceptionsCommandPublished();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnDecideIfItsTokenWasCancelledAsync()
        {
            // given: the mirror of the test above, thrown from the SAME dependency so that the
            // only difference between the two runs is the token carried by the exception. This
            // one WAS cancelled, so it is the caller's own withdrawal travelling back up and must
            // arrive untouched: wrapping it would turn a user navigating away into an error the
            // support channel is asked to explain, and logging it would fill the log with noise
            // nobody can act on.
            EntityType entityType = EntityType.Tag;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupDecideExceptionsAuthorisedPath(approvalId);

            SetupDecideExceptionsStorageRead(
                CreateDecideExceptionsApproval(approvalId, entityType, entityId));

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can
            // decide the branch — the entry guard cannot short-circuit this run.
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualException =
                await Assert.ThrowsAsync<OperationCanceledException>(decideTask.AsTask);

            // then: the very same instance, not an equivalent one — a rethrow, never a re-wrap.
            actualException.Should().BeSameAs(operationCanceledException);

            VerifyNoDecideExceptionsCommandPublished();

            // Not logged at all. The rethrow arm is the only arm of the chain that writes nothing
            // to the log, and that silence is the assertion.
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnDecideIfCancellationRequestedAsync()
        {
            // given: a token already cancelled when the call is made. The guard sits ahead of the
            // validations and the envelope, so an abandoned decision costs no storage read, no
            // policy evaluation and — above all — no write.
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalOutcome> decideTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(decideTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── suite-local helpers ─────────────────────────────────────────────────────────────
        // Named for this suite so they can never collide with the decision suites written
        // alongside it.

        // The happy path as far as the authorisation, and no further: the probe finds the row and
        // the one decision question permits it. What follows is whatever the individual test
        // arranges to fail.
        private void SetupDecideExceptionsAuthorisedPath(Guid approvalId)
        {
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());
        }

        private void SetupDecideExceptionsStorageRead(Approval storageApproval) =>
            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

        // The stored row as the read returns it: still Submitted, no bypass recorded. Every test
        // here fails before or during the write, so what these tests care about is only that a
        // real row exists for the write to be attempted against.
        private static Approval CreateDecideExceptionsApproval(
            Guid approvalId,
            EntityType entityType,
            Guid entityId) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = ApprovalStatus.Submitted,
                IsApprovedByBypass = false,
                ApprovedByBypassReason = null,
            };

        private void VerifyNoDecideExceptionsWrite()
        {
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoDecideExceptionsCommandPublished();
        }

        // No entity sync command left this service. The envelope broker was used ONCE — for the
        // caller envelope the authorisation runs against — and never again for the system
        // envelope a command would need, which together with a silent event broker is what
        // "nothing was announced" means here.
        private void VerifyNoDecideExceptionsCommandPublished()
        {
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Approval>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
