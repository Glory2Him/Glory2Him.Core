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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // The Added flow reaches FIVE dependency calls, and no single arrangement reaches them
        // all: the probe answers either "no row" (which inserts) or "a row" (which reads, and
        // reinstates only when the row was closed), and only a Submitted approval goes on to the
        // evaluation and its write. So each call is failed from an arrangement that actually
        // reaches it, rather than one representative case standing in for the rest.
        //
        // The catch chain is shared — ONE generic TryCatch serving the verdict read, the decision
        // and this flow — so the families cannot be mapped differently here. What these tests can
        // still catch is a call site reached OUTSIDE the chain, and a failure part-way through
        // that nonetheless leaves an entity sync command published or a row written. Those are
        // per-dependency facts, and only per-dependency tests state them.
        //
        // ModifyApprovalAsync is failed TWICE for that reason: the reinstate write (§9.7.2 rule 2)
        // and the auto-approve write (§9.7.7 rule 4) are the same method at two very different
        // points, and only the second one has a command behind it.

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a validation-shaped failure from the Approval foundation is a fault in what
            // this orchestration asked for, not in what the caller asked for, so it becomes a
            // DEPENDENCY validation exception carrying the foundation exception's OWN inner and
            // never the foundation type itself (§1.1.3 — no foundation exception leaks upward).
            EntityType entityType = EntityType.Tag;
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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // Every family on this service funnels through LogError — none escalates to
            // LogCritical — so the day one is "upgraded" a test says so rather than a log filter.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // The probe is the first call that can fail. Nothing may have been created, read or
            // written, and no policy may have been asked.
            VerifyNoAddedExceptionsWrite();

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the foundation's dependency and service failures are both external faults,
            // so they collapse to one orchestration dependency category.
            EntityType entityType = EntityType.Link;
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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsWrite();

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheInsertDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the probe found the key unoccupied, so the flow inserts the Draft row
            // (§9.7.2 rule 1) — and that insert is the one call in this flow whose validation
            // failure is genuinely likely, because the unique key spans soft-deleted rows. It
            // must still arrive as the orchestration's own dependency-validation family.
            EntityType entityType = EntityType.Reaction;
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsUnoccupiedKey();

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // An approval that was never created cannot be evaluated, and there is no row for a
            // second write to touch.
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The entity's status is read to decide what the round opens at (§9.7.2 rule 1).
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityApprovalStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheInsertDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the store was unreachable for the insert. The fact that triggered this flow
            // is not re-published and nothing downstream is told anything.
            EntityType entityType = EntityType.BibleReference;
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsUnoccupiedKey();

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The entity's status is read to decide what the round opens at (§9.7.2 rule 1).
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityApprovalStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the probe answers with a PROJECTION, so the row itself is read a second time
            // before anything can branch on it. That read is a distinct chance to fail and must be
            // caught by the same chain rather than escaping as a raw foundation exception.
            EntityType entityType = EntityType.Comment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // A row that could not be read must not be inserted a second time — the key is
            // occupied and that insert could never succeed (§9.7.2 rule 2).
            VerifyNoAddedExceptionsWrite();

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the read of the row the flow is about to branch on failed outright.
            EntityType entityType = EntityType.Association;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsWrite();

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheReinstateWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the probe found a CLOSED row, so the flow reinstates it in place rather than
            // inserting beside it (§9.7.2 rule 2). This write happens before any policy is read,
            // so its failure must end the flow with nothing asked of the access broker at all.
            EntityType entityType = EntityType.Tag;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsReinstatePath(approvalId, entityType, entityId);

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // The occupied key is never insert-attempted, and a reinstatement that did not land
            // resolves no approval — so the evaluation is never reached.
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheReinstateWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same guarantee for the external-fault families — a store that refused the
            // reinstatement left the approval closed, and a closed approval enters no round.
            EntityType entityType = EntityType.Link;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsReinstatePath(approvalId, entityType, entityId);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the mapping is a property of the TryCatch, not of which dependency tripped
            // it — the same families must land in the same category whether the Approval
            // foundation or the policy broker raised them.
            EntityType entityType = EntityType.Comment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

            SetupAddedExceptionsStorageRead(
                CreateAddedExceptionsApproval(approvalId, entityType, entityId));

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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // Conditions that could not be evaluated are not met conditions. §9.7.7 approves only
            // on a verdict, so a verdict that never arrived must approve nothing.
            VerifyNoAddedExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the §8.5 evaluation is the whole of the shared step (§9.7.7 rule 2), and it
            // could not be obtained. Staying Submitted is the only safe answer, and that means
            // writing nothing at all.
            EntityType entityType = EntityType.Reaction;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

            SetupAddedExceptionsStorageRead(
                CreateAddedExceptionsApproval(approvalId, entityType, entityId));

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddedIfTheApprovalWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the auto-approve write is the LAST thing that can fail before the entity sync
            // command is published, and it is the failure that matters most. §9.8 makes the
            // Approval row the source of truth; a command published for an approval that was never
            // stored would drive the entity to a state no approval records.
            EntityType entityType = EntityType.BibleReference;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsAutoApprovePath(approvalId, entityType, entityId);

            var expectedDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddedIfTheApprovalWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same guarantee for the external-fault families — a store that was
            // unreachable approved nothing, so nothing may be announced.
            EntityType entityType = EntityType.Association;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsAutoApprovePath(approvalId, entityType, entityId);

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamXeptionOnAddedAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: a Xeption from a collaborator this service names no catch for still has to be
            // categorized — the broad downstream catch calls it a dependency issue and re-surfaces
            // the failing exception's OWN inner, so no foreign exception type ever crosses the
            // layer boundary (§1.1.3).
            EntityType entityType = EntityType.Tag;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

            SetupAddedExceptionsStorageRead(
                CreateAddedExceptionsApproval(approvalId, entityType, entityId));

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
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddedIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything the service did not anticipate is its own fault until proven
            // otherwise — categorized as a SERVICE error rather than a dependency one, so a bug in
            // this orchestration is never filed against the collaborator it happened next to. The
            // raw Exception is thrown from the auto-approve WRITE, the deepest call in the flow,
            // to prove the broad catch sits outside the whole of it and not merely around its
            // first step.
            EntityType entityType = EntityType.Link;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsAutoApprovePath(approvalId, entityType, entityId);

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
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationServiceException>(
                    addedTask.AsTask);

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

            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddedIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose OWN token was never cancelled did not come from
            // the caller — it is a dependency that gave up, i.e. a timeout. The distinction is
            // drawn by the exception filter alone, and getting it backwards would report every
            // deliberate cancellation as a support-worthy failure while silently swallowing real
            // timeouts. That is precisely the pair this test and the next one exist to separate,
            // and it is asserted from the auto-approve WRITE — where a cancellation is most
            // consequential, because the approval may or may not have landed.
            EntityType entityType = EntityType.Comment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsAutoApprovePath(approvalId, entityType, entityId);

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
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can decide
            // the branch — the entry guard cannot short-circuit this run.
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    addedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAddedExceptionsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnAddedIfItsTokenWasCancelledAsync()
        {
            // given: the mirror of the test above, thrown from the SAME dependency so that the
            // only difference between the two runs is the token carried by the exception. This one
            // WAS cancelled, so it is a withdrawal travelling back up and must arrive untouched:
            // wrapping it would turn a shutting-down host into an error the support channel is
            // asked to explain, and logging it would fill the log with noise nobody can act on.
            EntityType entityType = EntityType.Comment;
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            SetupAddedExceptionsAutoApprovePath(approvalId, entityType, entityId);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so the entry guard cannot short-circuit this run
            // and only the thrown exception's own token can decide the branch.
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualException =
                await Assert.ThrowsAsync<OperationCanceledException>(addedTask.AsTask);

            // then: the very same instance, not an equivalent one — a rethrow, never a re-wrap.
            actualException.Should().BeSameAs(operationCanceledException);

            VerifyNoAddedExceptionsCommandPublished();

            // Not logged at all. The rethrow arm is the only arm of the chain that writes nothing
            // to the log, and that silence is the assertion.
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddedIfCancellationRequestedAsync()
        {
            // given: a token already cancelled when the call is made. The guard sits ahead of the
            // validations and of the resolution, so an abandoned reaction costs no probe, no
            // insert, no policy evaluation and — above all — no write.
            EntityType entityType = EntityType.Reaction;
            Guid entityId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalOutcome> addedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType,
                    entityId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(addedTask.AsTask);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── suite-local helpers ─────────────────────────────────────────────────────────────
        // Named for this suite so they can never collide with the Added suites written alongside
        // it. The shared fixture's CreateApprovalMatch cannot express a CLOSED row, which is the
        // whole of the reinstate branch, so the match factory is re-stated here rather than the
        // shared one widened.

        private static ApprovalEntityMatch CreateAddedExceptionsMatch(
            Guid approvalId,
            ApprovalStatus approvalStatus,
            bool isDeleted) =>
            new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
            };

        // The stored row as the read returns it. Submitted by default, because a Draft ends the
        // flow at §9.7.3 rule 1 and would leave every dependency past the resolution unreached.
        private static Approval CreateAddedExceptionsApproval(
            Guid approvalId,
            EntityType entityType,
            Guid entityId,
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted,
            bool isDeleted = false) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = false,
                ApprovedByBypassReason = null,
                IsDeleted = isDeleted,
            };

        // Met AND asked to be applied without a human click — the only combination §9.7.7 rule 4
        // approves on, and therefore the only one that reaches the write these tests fail. The
        // fixture's CreateMetConditions pins ShouldAutoApprove to false, which is rule 5's case.
        private static ApprovalConditionsVerdict CreateAddedExceptionsAutoApproveConditions() =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = true,
                ShouldResetStaleReviewsOnChange = false,
                BlockReason = AccessDenialReason.None,
                BlockReasons = new List<AccessDenialReason>(),
                ApprovalCount = 3,
                RequiredNumberOfApprovals = 2,
                UnresolvedApprovalCommentCount = 0,
                Explanation = GetRandomString(),
            };

        // The probe answers "the key is unoccupied", which is the branch that inserts.
        private void SetupAddedExceptionsUnoccupiedKey() =>
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalEntityMatch)null);

        private void SetupAddedExceptionsStorageRead(Approval storageApproval) =>
            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

        // A CLOSED row on an occupied key: the probe finds it, the read returns it, and the write
        // that follows is the reinstatement (§9.7.2 rule 2) rather than an approval.
        private void SetupAddedExceptionsReinstatePath(
            Guid approvalId,
            EntityType entityType,
            Guid entityId)
        {
            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: true));

            SetupAddedExceptionsStorageRead(
                CreateAddedExceptionsApproval(
                    approvalId,
                    entityType,
                    entityId,
                    ApprovalStatus.Submitted,
                    isDeleted: true));
        }

        // The flow as far as the auto-approve write, and no further: a live Submitted row whose
        // conditions are met and set to apply themselves. What follows is whatever the individual
        // test arranges to fail.
        private void SetupAddedExceptionsAutoApprovePath(
            Guid approvalId,
            EntityType entityType,
            Guid entityId)
        {
            SetupApprovalProbe(
                CreateAddedExceptionsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

            SetupAddedExceptionsStorageRead(
                CreateAddedExceptionsApproval(approvalId, entityType, entityId));

            SetupConditions(CreateAddedExceptionsAutoApproveConditions());
        }

        // Nothing was created and nothing was written, and nothing was announced either.
        private void VerifyNoAddedExceptionsWrite()
        {
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<WorkflowAttribution>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoAddedExceptionsCommandPublished();
        }

        // No entity sync command left this service. Unlike the decision, this flow builds no
        // CALLER envelope at all — it runs on a fact, not on a request, and the only envelope it
        // ever creates is the SYSTEM one a command rides in — so a silent envelope broker
        // alongside a silent event broker is exactly what "nothing was announced" means here.
        private void VerifyNoAddedExceptionsCommandPublished()
        {
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
