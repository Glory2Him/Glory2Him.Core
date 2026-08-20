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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // The two reactive flows had Logic coverage only. What follows is the rest of the
        // envelope for both of them: the shape checks that run before anything is touched, every
        // dependency they reach failing in every family, and the cancellation/timeout split.
        //
        // The catch chain in ApprovalOrchestrationService.Exceptions.cs was READ rather than
        // assumed, and its order is what these tests encode:
        //
        //   1. OperationCanceledException whose OWN token is NOT cancelled → TIMEOUT, wrapped in
        //      TimeoutApprovalOrchestrationException inside an orchestration DEPENDENCY exception.
        //   2. OperationCanceledException otherwise → rethrown untouched, and NOT logged.
        //   3. the orchestration's own Invalid/NotFound/NotSupported/Null/Unauthorized →
        //      VALIDATION.
        //   4. the APPROVAL foundation's four families → dependency-validation for the two
        //      validation-shaped ones, dependency for the other two.
        //   5. any OTHER Xeption → the broad downstream catch, which is a DEPENDENCY exception.
        //
        // Step 5 is the one worth stating out loud, because the ApprovalReview foundation is
        // named nowhere in the chain: an ApprovalReviewValidationException raised by the stale
        // review reset does NOT land in the dependency-validation family the way the Approval
        // foundation's twin does — it falls through to the broad catch and arrives as a
        // dependency exception. That is deliberate (§1.1.3 — no foundation exception crosses the
        // layer under its own name), and it is pinned here so a later hand-written catch for the
        // review foundation is an argued change rather than a silent recategorisation.

        // Every family the ApprovalReview foundation raises, in ONE set, because the broad catch
        // makes no distinction between them. Split into two sets it would read as though the
        // chain told them apart.
        public static TheoryData<Xeption> FlowsGuardsApprovalReviewExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new ApprovalReviewValidationException(
                    message: randomMessage, innerException: innerException),

                new ApprovalReviewDependencyValidationException(
                    message: randomMessage, innerException: innerException),

                new ApprovalReviewDependencyException(
                    message: randomMessage, innerException: innerException),

                new ApprovalReviewServiceException(
                    message: randomMessage, innerException: innerException),
            };
        }

        // ── validations: the modified flow ──────────────────────────────────────────────────

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifiedIfEntityTypeIsUndefinedAndLogItAsync()
        {
            // given: an integer outside the enum. EntityType is half the key the unfiltered probe
            // reads on AND the switch that routes the entity sync, so an unrecognized member left
            // unchecked would resolve somebody else's approval, possibly auto-approve it, and only
            // then discover it has no command route — the §9.8 divergence where the Approval row
            // and its entity disagree and nothing can reconcile them.
            //
            // 97 rather than a value near the enum, and the id is perfectly good, so the report
            // must name the type and only the type.
            var undefinedEntityType = (EntityType)97;
            Guid inputEntityId = Guid.NewGuid();

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityType),
                value: "Value is not a recognized entity type");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType: undefinedEntityType,
                    entityId: inputEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // Named explicitly rather than left to the blanket check below: the probe is the first
            // thing the flow does, and its absence is what proves the shape check precedes the
            // resolution rather than following it.
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNothingWasWrittenByTheFlowsGuards();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifiedIfEntityIdIsInvalidAndLogItAsync()
        {
            // given: an empty id names no row, so nothing can occupy the (EntityType, EntityId)
            // pair the probe keys on (§9.7.2 rule 3). Carried through, it would resolve — and
            // therefore CREATE, at Draft — an approval for a key belonging to no entity, and
            // §9.7.2 rule 1 makes that row permanent: only a submit moves it, and no submit can
            // arrive for something that does not exist.
            //
            // The type is Link rather than the zero member, so a validation that silently
            // defaulted its input could not pass by coincidence.
            EntityType inputEntityType = EntityType.Link;
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityId),
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType: inputEntityType,
                    entityId: invalidEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNothingWasWrittenByTheFlowsGuards();
        }

        [Fact]
        public async Task ShouldReportEveryInvalidModifiedInputInOnePassAndLogItAsync()
        {
            // given: both halves of the key malformed at once. Both are reported, for the same
            // reason the verdict returns every block reason rather than the first (§16.7.2) — and
            // here the reader is a subscriber's dead-letter record rather than a person, so a
            // report naming one of two faults sends whoever opens it back for a second round trip
            // through a fact that can never be replayed successfully.
            var undefinedEntityType = (EntityType)97;
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityType),
                value: "Value is not a recognized entity type");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.EntityId),
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType: undefinedEntityType,
                    entityId: invalidEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            VerifyNothingWasWrittenByTheFlowsGuards();
        }

        // ── validations: the review flow ────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldThrowValidationExceptionOnReviewRecordedIfApprovalIdIsInvalidAndLogItAsync()
        {
            // given: the review flow is keyed on the ROUND rather than on an entity, so the empty
            // id is the only shape it can refuse. It matters more here than it looks: the id is
            // handed straight to a read, and a store that answered an empty key with its first
            // row would have this flow evaluate — and possibly reject — an approval nobody
            // recorded a review against.
            //
            // The key is Approval.Id, not EntityId, and the two are different fields on the same
            // model; a report naming the wrong one sends the reader to the wrong half of the fact.
            var invalidApprovalId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(Approval.Id),
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId: invalidApprovalId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    reviewRecordedTask.AsTask);

            // then: the read never ran, which is the whole of the protection.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNothingWasWrittenByTheFlowsGuards();
        }

        // ── exceptions: the modified flow, dependency by dependency ─────────────────────────

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifiedIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a validation-shaped failure from the Approval foundation is a fault in what
            // this orchestration asked for rather than in what the caller asked for, so it becomes
            // a DEPENDENCY validation exception carrying the foundation exception's OWN inner and
            // never the foundation type itself (§1.1.3).
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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    modifiedTask.AsTask);

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

            // The probe is the first call that can fail: no row was read, no policy was asked,
            // and no review was touched.
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheProbeDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the foundation's dependency and service failures are both external faults, so
            // they collapse to one orchestration dependency category.
            EntityType entityType = EntityType.Comment;
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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifiedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the probe answers with a PROJECTION, so the row itself is read a second time
            // before the flow can do anything with it. That read is its own chance to fail, and it
            // must be caught by the same chain rather than escaping as a raw foundation exception.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateFlowsGuardsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    modifiedTask.AsTask);

            // then: a row that could not be read is not a row anything may be decided about.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the store was unreachable for the read of the row this flow is about to
            // branch on. The fact that triggered the flow is not re-published and nothing
            // downstream is told anything.
            EntityType entityType = EntityType.BibleReference;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            SetupApprovalProbe(
                CreateFlowsGuardsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifiedIfTheReinstateWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: an edit arriving for an entity whose approval was CLOSED. The key is occupied
            // and a second insert can never succeed, so the row is reinstated in place (§9.7.2
            // rule 2) — a real write on the modified flow, and the only one that happens before
            // any policy is read. Its failure must therefore end the flow with the access broker
            // never asked anything at all.
            EntityType entityType = EntityType.Reaction;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            SetupFlowsGuardsReinstatePath(approvalId, entityType, entityId);

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    modifiedTask.AsTask);

            // then: an occupied key is never insert-attempted, and a reinstatement that did not
            // land resolves no approval — so nothing past the resolution ran.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
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
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            VerifyNoFlowsGuardsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheReinstateWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same guarantee for the external-fault families — a store that refused the
            // reinstatement left the approval closed, and a closed approval enters no round.
            EntityType entityType = EntityType.Association;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            SetupFlowsGuardsReinstatePath(approvalId, entityType, entityId);

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

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
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            VerifyNoFlowsGuardsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnModifiedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the mapping is a property of the TryCatch rather than of which dependency
            // tripped it — the same families must land in the same category whether the Approval
            // foundation or the policy broker raised them.
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    modifiedTask.AsTask);

            // then: the verdict carries ShouldResetStaleReviewsOnChange, so a verdict that never
            // arrived cannot be read as "the reset is off" — no review may be dismissed on the
            // strength of an answer nobody got.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the §8.5 evaluation is the whole of the shared step (§9.7.7 rule 2) and it
            // could not be obtained. Staying where it is, is the only safe answer for the row, and
            // that means writing nothing at all.
            EntityType entityType = EntityType.Tag;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FlowsGuardsApprovalReviewExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheReviewListingDoesAndLogItAsync(
            Xeption reviewFoundationException)
        {
            // given: RequireReapprovalOnChange is on, so the flow lists every review to find the
            // ones this edit invalidates. The listing failed, which means the flow does not know
            // WHICH reviews are stale — and a re-read of the conditions on top of a dismissal that
            // never happened would evaluate against reviews the setting says no longer count.
            //
            // All four ApprovalReview families arrive as ONE category, because the chain names the
            // review foundation nowhere and the broad Xeption catch takes them (see the note at
            // the top of this file). The validation-shaped ones do NOT become dependency
            // validations the way the Approval foundation's do.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);
            SetupConditions(CreateFlowsGuardsConditions(shouldResetStaleReviewsOnChange: true));

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (reviewFoundationException.InnerException as Xeption)!);

            this.approvalReviewServiceMock.Setup(service =>
                service.RetrieveAllApprovalReviewsAsync(
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(reviewFoundationException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // Nothing was dismissed on the strength of a listing that failed, and the conditions
            // were read ONCE — the re-read exists only to follow a dismissal that happened.
            this.approvalReviewServiceMock.Verify(service =>
                service.DismissApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FlowsGuardsApprovalReviewExceptions))]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfTheReviewDismissalDoesAndLogItAsync(
            Xeption reviewFoundationException)
        {
            // given: the listing found stale reviews and the dismissal of one of them failed. This
            // is the worst place in the flow to stop — some reviews may already be dismissed and
            // others not — and the one thing that must NOT happen is the flow carrying on to a
            // re-read and an evaluation over a review set it left half-reset. It ends instead, and
            // the un-dismissed reviews stay countable until a later edit or a repair pass says
            // otherwise.
            EntityType entityType = EntityType.Comment;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);
            SetupConditions(CreateFlowsGuardsConditions(shouldResetStaleReviewsOnChange: true));

            SetupFlowsGuardsReviewListing(new List<ApprovalReview>
            {
                CreateFlowsGuardsReview(
                    approvalReviewId: staleReviewId,
                    approvalId: approvalId,
                    statusId: ApprovalStatus.Approved),
            });

            var expectedDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (reviewFoundationException.InnerException as Xeption)!);

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(reviewFoundationException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then: the dismissal was genuinely attempted — a test whose arrangement never reached
            // it would prove nothing about how its failure is handled.
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.approvalReviewServiceMock.Verify(service =>
                service.DismissApprovalReviewAsync(
                    staleReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // and there is no SECOND conditions read: the re-read is what an evaluation would run
            // on, and no evaluation may follow a half-finished reset.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamXeptionOnModifiedAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: a Xeption from a collaborator this service names no catch for still has to be
            // categorized — the broad downstream catch calls it a dependency issue and re-surfaces
            // the failing exception's OWN inner, so no foreign exception type ever crosses the
            // layer boundary (§1.1.3).
            EntityType entityType = EntityType.Tag;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);

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
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnModifiedIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything the service did not anticipate is its own fault until proven
            // otherwise — a SERVICE error rather than a dependency one, so a bug in this
            // orchestration is never filed against the collaborator it happened next to. The raw
            // Exception is thrown from the DISMISSAL, the deepest call the modified flow makes, to
            // prove the broad catch sits outside the whole of it rather than around its first step.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);
            SetupConditions(CreateFlowsGuardsConditions(shouldResetStaleReviewsOnChange: true));

            SetupFlowsGuardsReviewListing(new List<ApprovalReview>
            {
                CreateFlowsGuardsReview(
                    approvalReviewId: staleReviewId,
                    approvalId: approvalId,
                    statusId: ApprovalStatus.Approved),
            });

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

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationServiceException>(
                    modifiedTask.AsTask);

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

            VerifyNoFlowsGuardsApprovalWrite();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── exceptions: the review flow, dependency by dependency ───────────────────────────

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnReviewRecordedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the review flow opens on the read of its round, so this is the first call that
            // can fail and the one whose failure must cost nothing else.
            var approvalId = Guid.NewGuid();

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    reviewRecordedTask.AsTask);

            // then: the status gate is answered off the stored row, so a row that could not be
            // read leaves the flow with no way to know whether the round is even open.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnReviewRecordedIfTheStorageReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same read, failing for an external reason instead.
            var approvalId = Guid.NewGuid();

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnReviewRecordedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the round is open, so the flow asks the §8.5 question — and that question is
            // where BOTH branches of this flow come from. BlockReasons decides whether a standing
            // rejection ends the round; the rest decides whether the evaluation approves. A verdict
            // that never arrived is not "nothing is blocking", and must end the flow rather than
            // fall through to either branch.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(approvalId, EntityType.Link, entityId));

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    reviewRecordedTask.AsTask);

            // then: asked about the ROUND's own id, which is the id the fact named.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnReviewRecordedIfTheConditionsReadDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the policy engine was unreachable. Neither the rejection branch nor the
            // evaluation may run on a guess.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(approvalId, EntityType.ContentItem, entityId));

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnReviewRecordedIfTheRejectionWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a standing rejection under BlockOnReject ends the round, and the write that
            // ends it is the LAST thing that can fail before the entity is told (§9.7.5 rule 2).
            // §9.8 makes the Approval row the source of truth, so a command published for a
            // rejection that was never stored would drive the entity to a state no approval
            // records — and one nobody could later reconcile, because the repair direction is
            // approval-to-entity.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SetupFlowsGuardsStandingRejectionPath(approvalId, entityId);

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyValidationException>(
                    reviewRecordedTask.AsTask);

            // then: the write was genuinely attempted, so its failure is what this asserts about
            // rather than an arrangement that never reached it.
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsCommandPublished();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ApprovalDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnReviewRecordedIfTheRejectionWriteDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: the same guarantee for the external-fault families — a store that was
            // unreachable rejected nothing, so nothing may be announced.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SetupFlowsGuardsStandingRejectionPath(approvalId, entityId);

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsCommandPublished();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWrapADownstreamXeptionOnReviewRecordedAsAnOrchestrationDependencyAndLogItAsync()
        {
            // given: a Xeption this service names no catch for still has to be categorized, and
            // the broad downstream catch re-surfaces the failing exception's OWN inner so no
            // foreign type crosses the layer boundary (§1.1.3).
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(approvalId, EntityType.Link, entityId));

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnReviewRecordedIfServiceErrorOccursAndLogItAsync()
        {
            // given: an unanticipated failure is the service's own until proven otherwise. Raised
            // from the rejection WRITE — the deepest call this flow makes — so the broad catch is
            // shown to sit outside the whole flow rather than around its opening read.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SetupFlowsGuardsStandingRejectionPath(approvalId, entityId);

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationServiceException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsCommandPublished();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── cancellation and timeout: the two halves of one exception filter ────────────────

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnModifiedIfCancellationRequestedAsync()
        {
            // given: a token already cancelled when the call is made. The guard sits ahead of the
            // shape check and of the resolution, so an abandoned reaction costs no probe, no
            // insert, no policy evaluation, no dismissal and — above all — no write.
            EntityType entityType = EntityType.Reaction;
            Guid entityId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(modifiedTask.AsTask);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnModifiedIfItsTokenWasCancelledAsync()
        {
            // given: a cancellation raised MID-FLIGHT, from the dismissal loop, and carrying a
            // token that WAS cancelled. That makes it a withdrawal travelling back up, and it must
            // arrive untouched: wrapping it would turn a shutting-down host into an error the
            // support channel is asked to explain, and logging it would fill the log with noise
            // nobody can act on.
            //
            // The token handed IN is live, so only the exception's own token can decide the branch
            // — the entry guard cannot short-circuit this run and claim the credit.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);
            SetupConditions(CreateFlowsGuardsConditions(shouldResetStaleReviewsOnChange: true));

            SetupFlowsGuardsReviewListing(new List<ApprovalReview>
            {
                CreateFlowsGuardsReview(
                    approvalReviewId: staleReviewId,
                    approvalId: approvalId,
                    statusId: ApprovalStatus.Approved),
            });

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualException =
                await Assert.ThrowsAsync<OperationCanceledException>(modifiedTask.AsTask);

            // then: the very same instance, not an equivalent one — a rethrow, never a re-wrap.
            actualException.Should().BeSameAs(operationCanceledException);

            VerifyNoFlowsGuardsApprovalWrite();

            // Not logged at all. The rethrow arm is the only arm of the chain that writes nothing
            // to the log, and that silence is the assertion.
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnModifiedIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: the mirror of the test above, thrown from the SAME dependency so the only
            // difference between the two runs is the token the exception carries. This one was
            // never cancelled, so it did not come from the caller — it is a dependency that gave
            // up, i.e. a timeout. Getting the filter backwards would report every deliberate
            // cancellation as a support-worthy failure while silently swallowing real timeouts.
            EntityType entityType = EntityType.Link;
            Guid entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var staleReviewId = Guid.NewGuid();

            SetupFlowsGuardsResolvedRow(approvalId, entityType, entityId);
            SetupConditions(CreateFlowsGuardsConditions(shouldResetStaleReviewsOnChange: true));

            SetupFlowsGuardsReviewListing(new List<ApprovalReview>
            {
                CreateFlowsGuardsReview(
                    approvalReviewId: staleReviewId,
                    approvalId: approvalId,
                    statusId: ApprovalStatus.Approved),
            });

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

            this.approvalReviewServiceMock.Setup(service =>
                service.DismissApprovalReviewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so only the thrown exception's own token can decide
            // the branch.
            ValueTask<ApprovalOutcome> modifiedTask =
                this.approvalOrchestrationService.ProcessEntityModifiedAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    modifiedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsApprovalWrite();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnReviewRecordedIfCancellationRequestedAsync()
        {
            // given: a token already cancelled when the call is made. The review flow's guard sits
            // ahead of its shape check, so nothing is read and no round is touched.
            var approvalId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(reviewRecordedTask.AsTask);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnReviewRecordedIfItsTokenWasCancelledAsync()
        {
            // given: the cancellation is raised from the rejection WRITE, where it matters most —
            // the row may or may not have landed. It carries a cancelled token, so it is a
            // withdrawal and travels back up untouched and unlogged.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SetupFlowsGuardsStandingRejectionPath(approvalId, entityId);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when: a live token is handed in, so the entry guard cannot short-circuit this run
            // and only the thrown exception's own token can decide the branch.
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualException =
                await Assert.ThrowsAsync<OperationCanceledException>(reviewRecordedTask.AsTask);

            // then: the very same instance, not an equivalent one.
            actualException.Should().BeSameAs(operationCanceledException);

            VerifyNoFlowsGuardsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnReviewRecordedIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: the mirror of the rethrow test, from the same write, differing only in the
            // token the exception carries. Never cancelled means nobody withdrew — the store gave
            // up, and that is a timeout.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SetupFlowsGuardsStandingRejectionPath(approvalId, entityId);

            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalOrchestrationException =
                new TimeoutApprovalOrchestrationException(
                    message: "Failed content item association orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

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
            ValueTask<ApprovalOutcome> reviewRecordedTask =
                this.approvalOrchestrationService.ProcessApprovalInputsChangedAsync(
                    approvalId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationDependencyException>(
                    reviewRecordedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoFlowsGuardsCommandPublished();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── suite-local helpers ─────────────────────────────────────────────────────────────
        // Prefixed for this suite so they can never collide with the Flows suites written
        // alongside it. The shared fixture's CreateApprovalMatch cannot express a CLOSED row,
        // which is the whole of the reinstate branch, so the match factory is re-stated here
        // rather than the shared one widened.

        private static ApprovalEntityMatch CreateFlowsGuardsMatch(
            Guid approvalId,
            ApprovalStatus approvalStatus,
            bool isDeleted) =>
            new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
            };

        // Submitted by default: the review flow ends at its status gate for anything else, which
        // would leave every dependency past the gate unreached.
        private static Approval CreateFlowsGuardsApproval(
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

        private static ApprovalReview CreateFlowsGuardsReview(
            Guid approvalReviewId,
            Guid approvalId,
            ApprovalStatus statusId) =>
            new ApprovalReview
            {
                Id = approvalReviewId,
                ApprovalId = approvalId,
                StatusId = statusId,
                IsDeleted = false,
            };

        // Deliberately NOT auto-approving: these suites fail a dependency, and a verdict that also
        // approved would add a second write whose absence could be mistaken for the guarantee
        // under test. The counts differ from one another so a field read off the wrong one shows.
        private static ApprovalConditionsVerdict CreateFlowsGuardsConditions(
            bool shouldResetStaleReviewsOnChange = false,
            IReadOnlyList<AccessDenialReason> blockReasons = null)
        {
            IReadOnlyList<AccessDenialReason> resolvedBlockReasons =
                blockReasons ?? new List<AccessDenialReason>();

            return new ApprovalConditionsVerdict
            {
                AreConditionsMet = false,
                ShouldAutoApprove = false,
                ShouldResetStaleReviewsOnChange = shouldResetStaleReviewsOnChange,

                BlockReason = resolvedBlockReasons.Count == 0
                    ? AccessDenialReason.None
                    : resolvedBlockReasons[0],

                BlockReasons = resolvedBlockReasons,
                ApprovalCount = 1,
                RequiredNumberOfApprovals = 4,
                UnresolvedApprovalCommentCount = 2,
                Explanation = GetRandomString(),
            };
        }

        private void SetupFlowsGuardsStorageRead(Approval storageApproval) =>
            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

        // A LIVE row on an occupied key: the probe finds it, the read returns it, and the flow
        // carries straight on to the conditions with no write in between.
        private void SetupFlowsGuardsResolvedRow(
            Guid approvalId,
            EntityType entityType,
            Guid entityId)
        {
            SetupApprovalProbe(
                CreateFlowsGuardsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: false));

            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(approvalId, entityType, entityId));
        }

        // A CLOSED row on an occupied key: the write that follows the read is the reinstatement
        // (§9.7.2 rule 2) rather than anything to do with approving.
        private void SetupFlowsGuardsReinstatePath(
            Guid approvalId,
            EntityType entityType,
            Guid entityId)
        {
            SetupApprovalProbe(
                CreateFlowsGuardsMatch(approvalId, ApprovalStatus.Submitted, isDeleted: true));

            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(
                    approvalId,
                    entityType,
                    entityId,
                    ApprovalStatus.Submitted,
                    isDeleted: true));
        }

        // The listing answers with EVERY review in the store, as the real one does — handing back
        // only this approval's rows would move the filter into the fixture.
        private void SetupFlowsGuardsReviewListing(List<ApprovalReview> approvalReviews) =>
            this.approvalReviewServiceMock.Setup(service =>
                service.RetrieveAllApprovalReviewsAsync(
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvalReviews.AsQueryable());

        // The review flow as far as the rejection write, and no further: an open round whose
        // conditions report a standing rejection (§9.7.5 rule 2). What follows is whatever the
        // individual test arranges to fail.
        private void SetupFlowsGuardsStandingRejectionPath(Guid approvalId, Guid entityId)
        {
            SetupFlowsGuardsStorageRead(
                CreateFlowsGuardsApproval(approvalId, EntityType.Link, entityId));

            SetupConditions(
                CreateFlowsGuardsConditions(
                    blockReasons: new List<AccessDenialReason>
                    {
                        AccessDenialReason.BlockedByRejection,
                    }));
        }

        // Neither insert nor update reached the Approval row, and nothing was announced either.
        private void VerifyNoFlowsGuardsApprovalWrite()
        {
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNoFlowsGuardsCommandPublished();
        }

        // No entity sync command left this service. These flows run on a fact rather than on a
        // request, so the only envelope either ever creates is the SYSTEM one a command rides in
        // — a silent envelope broker beside a silent event broker is exactly what "nothing was
        // announced" means here.
        private void VerifyNoFlowsGuardsCommandPublished()
        {
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // Nothing at all happened past the shape check: no read, no policy, no review, no write,
        // no command. Used by the validation suites, where the whole claim is that the fact was
        // refused before it cost anything.
        private void VerifyNothingWasWrittenByTheFlowsGuards()
        {
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.approvalReviewServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
