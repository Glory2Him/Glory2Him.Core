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

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // Every answer the decision function can give that must NOT become a write. The null case
        // is the one worth spelling out: a broker that returned nothing at all — a policy the
        // client could not resolve — must read as a refusal, because the alternative is a
        // dereference that turns "we could not tell" into a decided approval.
        public static TheoryData<AccessVerdict> DecideRefusingVerdicts() =>
            new TheoryData<AccessVerdict>
            {
                null,
                RefusedVerdict(AccessDenialReason.SelfApprovalNotPermitted),
                RefusedVerdict(AccessDenialReason.ApprovalThresholdNotMet),
                RefusedVerdict(AccessDenialReason.NotInPublisherTier),
            };

        private static Approval CreateDecideStorageApproval(
            Guid approvalId,
            EntityType entityType,
            Guid entityId,
            ApprovalStatus approvalStatus) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = false,
                ApprovedByBypassReason = null,
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDecideIfEntityIdIsInvalidAndLogItAsync()
        {
            // given: an empty id names no row, so nothing can occupy the (EntityType, EntityId)
            // pair the probe keys on. The shape check runs before the envelope is created, which
            // is what keeps a malformed decision off the storage path entirely rather than
            // letting it reach the ONE authorisation and be refused there — a decision refused
            // by policy and a decision that never named a row are different events, and only the
            // second is the caller's typing mistake.
            EntityType inputEntityType = EntityType.Tag;
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
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: invalidEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: nothing was read, nothing was written, nothing was published. The envelope
            // broker is silent too, which is the proof the check precedes it.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDecideIfEntityTypeIsUndefinedAndLogItAsync()
        {
            // given: an integer outside the enum. EntityType is the other half of the key AND the
            // switch that routes the entity sync, so an unrecognized member is refused at the
            // front rather than carried to the command route, where it would surface as "the
            // approval was decided but its entity cannot be synchronised" — a far worse outcome
            // for input that could never have named a row in the first place.
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
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: undefinedEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: the id and the decision were both perfectly good, so only the type is
            // reported — a validation naming more would be reporting failures that did not happen.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDecideIfDecisionIsUndefinedAndLogItAsync()
        {
            // given: an integer outside ApprovalDecision. The decision is what picks the status
            // written to the source-of-truth row, and the service reads it as "Approve, or else
            // Rejected" — so an unrecognized member left unchecked would silently REJECT the
            // approval. Refused here, where the caller can still be told they sent nonsense.
            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();
            var undefinedDecision = (ApprovalDecision)42;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(ApprovalDecision),
                value: "Value is not a recognized approval decision");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: undefinedDecision,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReportEveryInvalidDecideInputInOnePassAndLogItAsync()
        {
            // given: both halves of the key malformed at once. Both are reported, for the same
            // reason the verdict returns every block reason rather than the first (§16.7.2) — a
            // caller told only about the type fixes it, retries, and only then learns about the
            // id they could have corrected in the same visit.
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

            // when: the decision itself is well-formed, so its key must be ABSENT from the
            // report — a validation that listed every rule it ran would be useless for fixing.
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: undefinedEntityType,
                    entityId: invalidEntityId,
                    decision: ApprovalDecision.Reject,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n ")]
        public async Task ShouldThrowValidationExceptionOnDecideIfBypassIsRequestedWithoutAReasonAndLogItAsync(
            string invalidBypassReason)
        {
            // given: a bypass asked for with nothing said about why. A bypass is only tolerable
            // because it leaves a record a moderator can later read, and an unexplained one
            // records nothing worth reading (§9.7.5).
            //
            // The policy here is set to PERMIT — both the plain question and the bypass one — so
            // the refusal cannot be mistaken for the policy doing the work. An unexplained bypass
            // must fail under every policy, INCLUDING one that would have granted the waiver,
            // which is only demonstrable if the permitting policy is in place and still unused.
            EntityType inputEntityType = EntityType.Comment;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch(approvalId: storedApprovalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: "bypassReason",
                value: "Reason is required when a bypass is requested");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    isBypassRequested: true,
                    bypassReason: invalidBypassReason,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: the access broker was never asked. Refusing before the policy read is what
            // makes the rule unconditional — a check placed after it would be silently skipped
            // wherever a setting already allowed bypassing.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // Nor was the approval even looked for: an unexplained bypass is refused on its face,
            // so it can never double as a probe for which keys carry approvals (§14.5).
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDecideWithoutABypassReasonWhenNoBypassIsRequestedAsync()
        {
            // given: no bypass asked for, and no reason supplied. The rule is conditional on the
            // REQUEST, so the ordinary decision — which is every decision — must not be made to
            // carry a reason for a waiver nobody sought. Guards the mirror-image mistake to the
            // theory above: a blanket "reason is required" would refuse the common path.
            //
            // The approval's own id and the entity's id are deliberately different values, so an
            // authorisation asked about the wrong one is visible.
            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            Approval storageApproval = CreateDecideStorageApproval(
                approvalId: storedApprovalId,
                entityType: inputEntityType,
                entityId: inputEntityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(approvalId: storedApprovalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: it went through, and the shape validation logged nothing.
            actualOutcome.Should().NotBeNull();

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    storedApprovalId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DecideRefusingVerdicts))]
        public async Task ShouldThrowValidationExceptionOnDecideIfTheAccessDecisionRefusesAndLogItAsync(
            AccessVerdict refusingVerdict)
        {
            // given: the ONE authorisation in the flow says no. The refusal is turned into this
            // layer's own exception WITHOUT re-deriving the reason — repeating why here would put
            // the policy in a second place beside the function that owns it (§8.6.1 rule 4) — so
            // every refusing verdict yields the same sentence regardless of its denial code.
            //
            // The stored approval's id and the entity's id are pinned to different values so an
            // authorisation asked about the caller's entity instead of the row the probe found
            // cannot pass unnoticed.
            EntityType inputEntityType = EntityType.Tag;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch(approvalId: storedApprovalId));

            SetupAccessDecisions(
                decisionVerdict: refusingVerdict,
                bypassVerdict: refusingVerdict);

            var unauthorizedApprovalOrchestrationException =
                new UnauthorizedApprovalOrchestrationException(
                    message: "The current user is not allowed to decide this approval.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: the question was asked about the row the probe found, and the refusal stopped
            // everything after it — the source-of-truth row is untouched and no command went out.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    storedApprovalId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The route this entity type would have taken, proved untaken — and then the whole
            // broker, so a refusal that leaked out through some OTHER entity's command is caught
            // as well.
            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Glory2Him.Core.Models.Foundations.Tags.Tag>>(),
                    It.IsAny<Glory2Him.Core.Models.Events.Foundations.TagEventOperation>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDecideIfNoApprovalOccupiesTheKeyAndLogItAsync()
        {
            // given: the unfiltered probe finds the pair unoccupied. Reported as not-found rather
            // than created on the spot: a decision is a verdict ON a submission, and inventing the
            // row it is supposed to be deciding would record an approval nobody ever submitted.
            //
            // The type is Reaction and the id fresh, so the message is proved to name the key
            // that was asked about rather than a default.
            EntityType inputEntityType = EntityType.Reaction;
            Guid inputEntityId = Guid.NewGuid();

            SetupApprovalProbe(null);

            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {inputEntityType} with id: {inputEntityId}.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: the access broker was never consulted. The authorisation is asked about a
            // stored approval id (§16.7.1), and there is none — a question asked on Guid.Empty
            // would be a lookup the broker cannot answer, and whatever it answered would be a
            // decision about nothing.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDecideIfTheEntityTypeHasNoCommandRouteAndLogItAsync()
        {
            // given: Attachment — a DEFINED, approvable entity type with no command route in the
            // publish switch. It passes the shape check (the enum member exists) and reaches the
            // sync, which refuses rather than returning quietly: a decided Approval row whose
            // entity is never told would diverge the two records (§9.8) with nothing to show
            // for it.
            //
            // What this test pins is the REAL behaviour of the row, which is easy to assume
            // wrongly: the decision is written FIRST and is NOT rolled back. The failure is
            // therefore partial by construction — approval decided, entity un-synced — and the
            // exception message says exactly that. Nothing compensates, so a repair pass driving
            // the entity to match the approval remains the recovery, which is only possible
            // because the approval kept its decision.
            EntityType inputEntityType = EntityType.Attachment;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            Approval storageApproval = CreateDecideStorageApproval(
                approvalId: storedApprovalId,
                entityType: inputEntityType,
                entityId: inputEntityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(approvalId: storedApprovalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // The service hands the very instance it retrieved to the modify call, so the saved
            // state is snapshotted HERE, at the moment of the write, into plain value copies.
            // Reading it off the object afterwards would compare the object with itself.
            ApprovalStatus? savedApprovalStatus = null;
            Guid? savedApprovalId = null;

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .Callback((Approval approval, CancellationToken _) =>
                        {
                            savedApprovalStatus = approval.ApprovalStatus;
                            savedApprovalId = approval.Id;
                        })
                        .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            var notSupportedApprovalOrchestrationException =
                new NotSupportedApprovalOrchestrationException(
                    message: $"No approval command route is defined for " +
                        $"{inputEntityType}. The approval was decided but its entity " +
                        "cannot be synchronised.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notSupportedApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> decideApprovalTask =
                this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    decision: ApprovalDecision.Approve,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    decideApprovalTask.AsTask);

            // then: the NotSupported family surfaces as a VALIDATION exception, not a service
            // one — an entity type without a route is a request this service cannot serve, which
            // is the caller's answer to receive rather than an internal fault.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // The row WAS decided, once, and the decision stands. Retrieved as Submitted, saved
            // as Approved — two different values, so a write that never happened cannot pass.
            savedApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedApprovalId.Should().Be(storedApprovalId);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Nothing compensating: no second write reverting the status, and no removal of the
            // row. A rollback here is what would make the repair pass wrong.
            this.approvalServiceMock.Verify(service =>
                service.RemoveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.HardRemoveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // And no command went out on any route — the whole point of refusing rather than
            // guessing which entity should be told.
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
