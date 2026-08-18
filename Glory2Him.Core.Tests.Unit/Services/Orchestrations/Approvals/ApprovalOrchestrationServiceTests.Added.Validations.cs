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
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnProcessEntityAddedIfEntityIdIsInvalidAndLogItAsync()
        {
            // given: an empty id names no row, so nothing can occupy the (EntityType, EntityId)
            // pair the unfiltered probe keys on (§9.7.2 rule 3). The shape check runs before the
            // resolution, which is what keeps a malformed fact off the storage path entirely —
            // an empty id carried through would create an approval at Draft for a key that
            // belongs to no entity, and §9.7.2 rule 1 makes that row permanent: only the submit
            // action moves it, and no submit can ever arrive for an entity that does not exist.
            //
            // The type is Tag rather than the zero member, so a validation that silently
            // defaulted its input could not pass by coincidence.
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
            ValueTask<ApprovalOutcome> processEntityAddedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: inputEntityType,
                    entityId: invalidEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    processEntityAddedTask.AsTask);

            // then: the probe never ran. Named explicitly rather than left to the blanket check
            // below, because the probe is the first thing the flow does and the one call whose
            // absence proves the validation precedes resolution rather than following it.
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

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnProcessEntityAddedIfEntityTypeIsUndefinedAndLogItAsync()
        {
            // given: an integer outside the enum. EntityType is the other half of the key AND the
            // switch that routes the entity sync, so an unrecognized member left unchecked would
            // resolve an approval, possibly auto-approve it, and only then discover it has no
            // command route — the §9.8 divergence the flow exists to prevent. Refused at the
            // front, where the fact has cost nothing.
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
            ValueTask<ApprovalOutcome> processEntityAddedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: undefinedEntityType,
                    entityId: inputEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    processEntityAddedTask.AsTask);

            // then: the id was perfectly good, so only the type is reported — a validation naming
            // more would be reporting failures that did not happen.
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

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
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
        public async Task ShouldReportEveryInvalidProcessEntityAddedInputInOnePassAndLogItAsync()
        {
            // given: both halves of the key malformed at once. Both are reported, for the same
            // reason the verdict returns every block reason rather than the first (§16.7.2) — and
            // here the audience is a subscriber's dead-letter record rather than a person, so a
            // report naming one of two faults sends whoever reads it back for a second round trip
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
            ValueTask<ApprovalOutcome> processEntityAddedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: undefinedEntityType,
                    entityId: invalidEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    processEntityAddedTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnProcessEntityAddedIfTheConditionsVerdictIsNullAndLogItAsync()
        {
            // given: the resolution answered with a Submitted approval, but by the time the shared
            // evaluation (§9.7.7) asked what was blocking it, the row was gone — a concurrent hard
            // removal between the two reads. The broker reports that as null rather than as an
            // empty verdict precisely so it can be told apart from "nothing is blocking", and the
            // difference is everything here: a null dereferenced into AreConditionsMet would
            // instead be read as conditions unmet, and a null treated as a met-and-auto-approve
            // verdict would APPROVE and publish a row nobody can find.
            //
            // The approval's own id and the entity's id are pinned to different values, so an
            // evaluation asked about the entity instead of the approval row the probe found is
            // visible rather than hidden behind a shared variable.
            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            Approval storageApproval = CreateAddedGuardApproval(
                approvalId: storedApprovalId,
                entityType: inputEntityType,
                entityId: inputEntityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(
                CreateApprovalMatch(
                    approvalStatus: ApprovalStatus.Submitted,
                    approvalId: storedApprovalId));

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            SetupConditions(null);

            // The sentence names the key carried on the STORED row — the flow's own subject —
            // which for a genuine resolution is the pair the fact arrived with.
            var notFoundApprovalOrchestrationException =
                new NotFoundApprovalOrchestrationException(
                    message: $"Approval not found for {inputEntityType} with id: {inputEntityId}.");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalOrchestrationException);

            // when
            ValueTask<ApprovalOutcome> processEntityAddedTask =
                this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    processEntityAddedTask.AsTask);

            // then: the evaluation was asked about the row the probe found, and its unanswerable
            // reply stopped everything after it — the source-of-truth row is untouched and no
            // command went out to any entity.
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    storedApprovalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldProcessEntityAddedWithNoCallerIdentityGateAsync()
        {
            // given: the most hostile caller the fixture can express — unauthenticated, holding
            // not one role. It still drives the flow to a completed automatic approval, and that
            // is CORRECT rather than a hole: this is a reaction to a fact the system already
            // committed, not a user action. The trust boundary sat at the verified envelope that
            // carried the -Added fact; re-asking "may this caller approve" here would gate a
            // §9.7.7 evaluation that no human requested, and would refuse every automatic approval
            // raised by a background subscriber — which is all of them.
            //
            // Read the flow before changing this test: ProcessEntityAddedAsync asks no
            // authorisation question, and the command it publishes goes out under the WORKFLOW's
            // identity via CreateSystemAsync. This test pins that absence deliberately, so a gate
            // added later fails here and is argued for rather than assumed.
            //
            // Ids are pinned to different values and the counts on the verdict differ from each
            // other, so a field carried from the wrong source cannot coincide with the right one.
            EntityType inputEntityType = EntityType.Link;
            Guid inputEntityId = Guid.NewGuid();
            var storedApprovalId = Guid.NewGuid();

            this.ambientSecurityContext = new SecurityContext
            {
                IsAuthenticated = false,
                Roles = new string[0],
            };

            Approval storageApproval = CreateAddedGuardApproval(
                approvalId: storedApprovalId,
                entityType: inputEntityType,
                entityId: inputEntityId,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(
                CreateApprovalMatch(
                    approvalStatus: ApprovalStatus.Submitted,
                    approvalId: storedApprovalId));

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            SetupConditions(CreateAddedGuardAutoApproveConditions());
            List<Approval> savedApprovals = SetupAddedGuardApprovalSave();
            SetupAddedGuardLinkSystemEnvelope();
            List<EventEnvelope<Link>> publishedCommands = SetupAddedGuardLinkCommandPublish();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    entityType: inputEntityType,
                    entityId: inputEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: the outcome is asserted against values pinned BEFORE the act, never against
            // the object handed to the save — the service mutates the row it retrieved and passes
            // that same instance on, so reading it back afterwards would compare it with itself.
            actualOutcome.ApprovalId.Should().Be(storedApprovalId);
            actualOutcome.EntityId.Should().Be(inputEntityId);
            actualOutcome.EntityType.Should().Be(EntityType.Link);
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();

            savedApprovals.Should().ContainSingle();
            savedApprovals[0].ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedApprovals[0].Id.Should().Be(storedApprovalId);
            publishedCommands.Should().ContainSingle();

            // No authorisation question was asked of anybody. This is the assertion the whole test
            // exists for.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // Nor was the ambient caller ever captured: CreateAsync is the envelope that carries
            // the current user, and the flow reaches for CreateSystemAsync instead.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Approval>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private static Approval CreateAddedGuardApproval(
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

        // Conditions met AND the auto-approve flag set — the only combination §9.7.7 rules 3 to 5
        // let through without a human click. The counts are pinned to values that differ from one
        // another so a field read from the wrong one is visible.
        private static ApprovalConditionsVerdict CreateAddedGuardAutoApproveConditions() =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = true,
                BlockReason = AccessDenialReason.None,
                BlockReasons = new List<AccessDenialReason>(),
                ApprovalCount = 3,
                RequiredNumberOfApprovals = 2,
                UnresolvedApprovalCommentCount = 0,
                Explanation = GetRandomString(),
            };

        // The save captures a SNAPSHOT rather than the instance handed to it. The service mutates
        // the row it retrieved and passes that same object on, so a list holding the original
        // would be reading whatever the service wrote into it afterwards. The clone is also what
        // travels onward, matching a real store returning its own copy of the row.
        private List<Approval> SetupAddedGuardApprovalSave()
        {
            var savedApprovals = new List<Approval>();

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval, CancellationToken cancellationToken) =>
                        {
                            Approval savedApproval = approval.DeepClone();
                            savedApprovals.Add(savedApproval);

                            return new ValueTask<Approval>(savedApproval);
                        });

            return savedApprovals;
        }

        private void SetupAddedGuardLinkSystemEnvelope() =>
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()))
                    .Returns((Link content) =>
                        new ValueTask<EventEnvelope<Link>>(
                            new EventEnvelope<Link>
                            {
                                Content = content,
                                SecurityContext = new SecurityContext(),
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

        private List<EventEnvelope<Link>> SetupAddedGuardLinkCommandPublish()
        {
            var publishedCommands = new List<EventEnvelope<Link>>();

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns((EventEnvelope<Link> envelope, LinkEventOperation operation) =>
                        {
                            publishedCommands.Add(envelope);

                            return new ValueTask<EventPublishResult<Link>>(
                                new EventPublishResult<Link>());
                        });

            return publishedCommands;
        }
    }
}
