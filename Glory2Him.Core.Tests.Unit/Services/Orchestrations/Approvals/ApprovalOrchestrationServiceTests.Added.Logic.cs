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
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldCreateTheApprovalAtDraftWhenTheEntityKeyIsUnoccupiedAsync()
        {
            // given: nothing occupies (EntityType, EntityId), so resolution inserts. A newly
            // created approval is born DRAFT and never Submitted — only the submit action moves
            // it there (§9.7.2 rule 1), so creating one at Submitted would let content enter
            // review without anyone having offered it. The insert is captured as a SNAPSHOT taken
            // inside the call, because the service builds the row inline and the store's answer
            // is a different object.
            var createdApprovalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupApprovalProbe(approvalMatch: null);

            List<Approval> insertedApprovals =
                SetupAddedApprovalInsert(createdApprovalId: createdApprovalId);

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            insertedApprovals.Should().ContainSingle();
            Approval insertedApproval = insertedApprovals.Single();

            // Asserted against the argument the service actually handed the store, not against
            // the row it got back — a service that inserted Submitted and was answered with a
            // Draft row would satisfy an assertion written on the answer.
            insertedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
            insertedApproval.ApprovalStatus.Should().NotBe(ApprovalStatus.Submitted);

            // The key is echoed onto the row, and the entity type is deliberately not the enum's
            // zero member — a row left at its default would match ContentItem for free.
            insertedApproval.EntityType.Should().Be(EntityType.Link);
            insertedApproval.EntityId.Should().Be(entityId);

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Nothing is read or written besides the insert. An unoccupied key has no row to
            // retrieve and none to reinstate.
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
        }

        [Fact]
        public async Task ShouldUseTheStoredApprovalWhenTheKeyIsOccupiedAndNotDeletedAsync()
        {
            // given: the key is already occupied by a live row. A second insert can never succeed
            // against UX_Approvals_EntityType_EntityId (§9.7.2 rule 2), and the stored row is
            // addressed by the PROBE's id — a different value from the entity id on purpose, so a
            // read that went looking by the entity id cannot hide behind a shared variable.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Draft);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Draft, approvalId));
            SetupAddedApprovalRow(storageApproval);

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // A live row is used as it stands — reinstatement is for closed rows only, and a
            // write here would touch a row nothing asked to change.
            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            actualOutcome.ApprovalId.Should().Be(approvalId);
            actualOutcome.ApprovalId.Should().NotBe(entityId);
            actualOutcome.EntityId.Should().Be(entityId);
        }

        [Fact]
        public async Task ShouldReinstateTheSoftDeletedApprovalInPlaceAsync()
        {
            // given: a closed approval still OCCUPIES the key, because the unique index is not
            // filtered on IsDeleted, so it is reinstated in place rather than re-inserted (§9.7.2
            // rule 2). The row arrives carrying real deletion values, so clearing them is
            // observed rather than coinciding with fields that were already null.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var deletedWhen = new DateTimeOffset(2026, 3, 14, 9, 15, 0, TimeSpan.Zero);

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Draft,
                isDeleted: true,
                deletedBy: "the-remover",
                deletedWhen: deletedWhen,
                deletionReason: "taken down pending review");

            SetupApprovalProbe(
                CreateAddedDeletedApprovalMatch(
                    approvalId: approvalId,
                    approvalStatus: ApprovalStatus.Draft));

            List<Approval> savedApprovals = SetupAddedApprovalRow(storageApproval);

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: the saved row is a SNAPSHOT taken inside the save. The service mutates the
            // very object it retrieved and hands that same instance on, so asserting against the
            // row this test supplied would compare it with itself and pass whatever happened.
            savedApprovals.Should().ContainSingle();
            Approval savedApproval = savedApprovals.Single();

            savedApproval.IsDeleted.Should().BeFalse();
            savedApproval.DeletedBy.Should().BeNull();
            savedApproval.DeletedWhen.Should().BeNull();
            savedApproval.DeletionReason.Should().BeNull();

            // IN PLACE — the same row, addressed by the probe's id.
            savedApproval.Id.Should().Be(approvalId);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and never a second insert, which the index would refuse anyway.
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The review history is deliberately left intact: an entity restored after a takedown
            // resumes where it left off (§9.7.6).
            savedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
        }

        [Fact]
        public async Task ShouldResolveTheApprovalThroughTheUnfilteredEntityProbeAsync()
        {
            // given: resolution must not use a caller-facing read. Those are visibility-filtered
            // and answer "does not exist" for a soft-deleted row that does exist — inviting an
            // insert the unique index can never accept (§9.7.2 rule 3).
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Draft);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Draft, approvalId));
            SetupAddedApprovalRow(storageApproval);

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then: the key is looked up by the pair the index is on, over the unfiltered probe.
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    EntityType.Link,
                    entityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // A Times.Never on RetrieveAllApprovalsAsync stood here, pinning that the filtered
            // caller-facing listing is never how the key was tested. The orchestration now holds
            // IApprovalWorkflowService, which has no such member, so the type system says it —
            // verified: substituting the call fails to compile with CS1061 (#287).
        }

        [Fact]
        public async Task ShouldEndTheAddedFlowWithoutEvaluatingWhenTheApprovalIsDraftAsync()
        {
            // given: a Draft has not entered a round, so no policy is resolved and nothing can be
            // approved — the record exists only so the later submit has something to transition.
            // Stopping here is the whole of the branch, not an optimisation (§9.7.3 rule 1).
            var createdApprovalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupApprovalProbe(approvalMatch: null);
            SetupAddedApprovalInsert(createdApprovalId: createdApprovalId);

            // Armed on purpose. If the flow were to reach the evaluation it would find a verdict
            // that auto-approves, so a short-circuit that failed would fail loudly.
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalId.Should().Be(createdApprovalId);
            actualOutcome.ApprovalId.Should().NotBe(entityId);
            actualOutcome.EntityId.Should().Be(entityId);
            actualOutcome.EntityType.Should().Be(EntityType.Link);
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Draft);

            // Nothing was asked of the entity, so nothing is claimed to have been asked.
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldEvaluateTheApprovalWhenTheResolvedRowIsSubmittedAsync()
        {
            // given: an entity added straight into review. The evaluation is addressed by the
            // APPROVAL's id, which is a different value from the entity's — a call keyed by the
            // entity would ask the policy about a row it has never heard of.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedApprovalRow(storageApproval);
            SetupConditions(CreateAddedUnmetConditions());

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldEndTheAddedFlowForEveryStatusThatIsNotSubmittedAsync(
            ApprovalStatus approvalStatus)
        {
            // given: only Submitted is evaluated. A Draft has not entered a round, and a terminal
            // row has left one — an evaluation against either would resolve policy for a round
            // nobody is running, and an auto-approve reached from a terminal row would re-decide
            // an approval that was already decided.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: approvalStatus);

            SetupApprovalProbe(CreateApprovalMatch(approvalStatus, approvalId));
            SetupAddedApprovalRow(storageApproval);

            // Armed, so a short-circuit that leaked would auto-approve and be caught.
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(approvalStatus);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldLeaveTheApprovalSubmittedWhenTheConditionsAreNotMetAsync()
        {
            // given: the §8.5 conditions are unmet, so the approval STAYS Submitted (§9.7.7 rule
            // 3). A blocked entity is not Rejected, and nothing is written at all — reviewing
            // simply continues.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedApprovalRow(storageApproval);
            SetupConditions(CreateAddedUnmetConditions());

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldLeaveTheApprovalSubmittedWhenTheConditionsAreMetButAutoApproveIsOffAsync()
        {
            // given: the conditions ARE met and the policy still asks for a human click, so the
            // approval stays Submitted and the manual approve becomes available to Publisher /
            // Admin (§9.7.7 rule 5). Approving here would be the system taking a decision the
            // policy reserved for a person.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedApprovalRow(storageApproval);
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: false));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualOutcome.ApprovalStatus.Should().NotBe(ApprovalStatus.Approved);
            actualOutcome.IsEntitySyncRequested.Should().BeFalse();

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // and no command reaches the entity, because nothing about it changed.
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldApproveWithoutRecordingABypassWhenTheConditionsAreMetAndAutoApproveIsOnAsync()
        {
            // given: the conditions are met AND the policy asks for the click to be skipped, so
            // Approved is applied automatically (§9.7.7 rule 4). The waiver pair must read
            // false/null — an automatic approval fires precisely BECAUSE the conditions were met,
            // so recording a bypass would put a waiver on the one approval that provably needed
            // none. The stored row arrives already carrying a stale waiver, so clearing it is
            // observed rather than coinciding with fields that were already empty.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted,
                isApprovedByBypass: true,
                approvedByBypassReason: "stale bypass reason");

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedSystemEnvelope<Link>();
            List<Approval> savedApprovals = SetupAddedApprovalRow(storageApproval);
            List<EventEnvelope<Link>> publishedCommands = SetupAddedLinkCommandPublish();
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: true));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then: a SNAPSHOT of the saved row, because the service mutates the instance it
            // retrieved and hands that same object to the save.
            savedApprovals.Should().ContainSingle();
            Approval savedApproval = savedApprovals.Single();

            savedApproval.Id.Should().Be(approvalId);
            savedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedApproval.IsApprovedByBypass.Should().BeFalse();
            savedApproval.ApprovedByBypassReason.Should().BeNull();

            // The reason must be CLEARED alongside the flag. A row reading "not bypassed" beside
            // a bypass reason is the record contradicting itself.
            savedApproval.ApprovedByBypassReason.Should().NotBe("stale bypass reason");

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the entity is told, so §9.8's two records do not diverge.
            publishedCommands.Should().ContainSingle();

            actualOutcome.ApprovalId.Should().Be(approvalId);
            actualOutcome.EntityId.Should().Be(entityId);
            actualOutcome.EntityType.Should().Be(EntityType.Link);
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualOutcome.IsApprovedByBypass.Should().BeFalse();
            actualOutcome.ApprovedByBypassReason.Should().BeNull();

            // REQUESTED, not confirmed — the command travels as an event (§16.7.1).
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldMintTheAutomaticApprovalCommandUnderTheSystemIdentityAsync()
        {
            // given: nobody clicked, so there is no caller to publish under — the command is
            // minted under the WORKFLOW's identity. The approval's id and the entity's id are
            // DIFFERENT values: the command addresses the ENTITY, and a payload keyed by the
            // approval id would name a row the receiving transition has never heard of.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedSystemEnvelope<Link>();
            SetupAddedApprovalRow(storageApproval);
            List<EventEnvelope<Link>> publishedCommands = SetupAddedLinkCommandPublish();
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: true));

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            publishedCommands.Should().ContainSingle();
            Link publishedLink = publishedCommands.Single().Content;

            publishedLink.Id.Should().Be(entityId);
            publishedLink.Id.Should().NotBe(approvalId);
            publishedLink.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            publishedLink.IsApprovedByBypass.Should().BeFalse();
            publishedLink.ApprovedByBypassReason.Should().BeNull();

            // On the entity's own channel, under the Approving operation. A command delivered
            // elsewhere approves the Approval row and leaves its entity behind (§9.8).
            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkProcessingEventOperation.Approving),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWriteTheApprovalRowBeforePublishingTheCommandOnTheAddedFlowAsync()
        {
            // given: §9.8 names Approval.ApprovalStatus the source of truth, so it is written
            // FIRST and the entity follows. Entity-first would make a repair pass — which can only
            // mean "drive the entity to match the approval" — revert an approval that really
            // happened. Both sides stamp a shared counter, so the order is observed rather than
            // assumed from the two calls merely having occurred.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            int addedStep = 0;
            int approvalRowWrittenAt = 0;
            int entityCommandPublishedAt = 0;

            Approval storageApproval = CreateAddedApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupAddedSystemEnvelope<Link>();
            SetupConditions(CreateAddedMetConditions(shouldAutoApprove: true));

            SetupAddedApprovalRow(
                storageApproval,
                onApprovalSaved: () => approvalRowWrittenAt = ++addedStep);

            SetupAddedLinkCommandPublish(
                onCommandPublished: () => entityCommandPublishedAt = ++addedStep);

            // when
            await this.approvalOrchestrationService.ProcessEntityAddedAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            approvalRowWrittenAt.Should().Be(1);
            entityCommandPublishedAt.Should().Be(2);
        }

        private static Approval CreateAddedApproval(
            Guid approvalId,
            Guid entityId,
            EntityType entityType,
            ApprovalStatus approvalStatus,
            bool isApprovedByBypass = false,
            string approvedByBypassReason = null,
            bool isDeleted = false,
            string deletedBy = null,
            DateTimeOffset? deletedWhen = null,
            string deletionReason = null) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = isApprovedByBypass,
                ApprovedByBypassReason = approvedByBypassReason,
                IsDeleted = isDeleted,
                DeletedBy = deletedBy,
                DeletedWhen = deletedWhen,
                DeletionReason = deletionReason,
            };

        // The shared match helper only makes LIVE rows. A closed one is the case reinstatement
        // exists for, so it gets its own maker rather than a mutated copy of the other.
        private static ApprovalEntityMatch CreateAddedDeletedApprovalMatch(
            Guid approvalId,
            ApprovalStatus approvalStatus) =>
            new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = approvalStatus,
                IsDeleted = true,
            };

        // Met conditions with the auto-approve flag under the test's control — the shared maker
        // pins it false, and §9.7.7 turns on the two being independent. Counts are pinned to
        // DIFFERENT values from one another so a verdict read that crossed two fields cannot
        // pass on a coincidence.
        private static ApprovalConditionsVerdict CreateAddedMetConditions(
            bool shouldAutoApprove) =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = shouldAutoApprove,
                ShouldResetStaleReviewsOnChange = false,
                BlockReason = AccessDenialReason.None,
                BlockReasons = new List<AccessDenialReason>(),
                ApprovalCount = 3,
                RequiredNumberOfApprovals = 2,
                UnresolvedApprovalCommentCount = 0,
                Explanation = GetRandomString(),
            };

        // Conditions that block, with ShouldAutoApprove false as the real evaluation would answer
        // — auto-approve applies Approved once the conditions ALREADY are met, and never waives
        // them (§9.7.7).
        private static ApprovalConditionsVerdict CreateAddedUnmetConditions() =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = false,
                ShouldAutoApprove = false,
                ShouldResetStaleReviewsOnChange = false,
                BlockReason = AccessDenialReason.ApprovalThresholdNotMet,
                BlockReasons = new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                },
                ApprovalCount = 1,
                RequiredNumberOfApprovals = 4,
                UnresolvedApprovalCommentCount = 2,
                Explanation = GetRandomString(),
            };

        // The save is captured as a SNAPSHOT rather than as the instance handed to it. The service
        // mutates the row it retrieved and passes that same object on, so a test holding the
        // original would be reading whatever the service wrote into it and asserting against
        // itself. The clone is also what travels onward, matching a real store returning its own
        // copy of the row.
        private List<Approval> SetupAddedApprovalRow(
            Approval storageApproval,
            Action onApprovalSaved = null)
        {
            var savedApprovals = new List<Approval>();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.approvalServiceMock.Setup(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval, CancellationToken cancellationToken) =>
                        {
                            Approval savedApproval = approval.DeepClone();
                            savedApprovals.Add(savedApproval);
                            onApprovalSaved?.Invoke();

                            return new ValueTask<Approval>(savedApproval);
                        });

            return savedApprovals;
        }

        // The insert is captured the same way, and the store answers with a DIFFERENT instance
        // carrying the id it assigned — as a real one does. A mock echoing the argument back
        // would let a test assert the inserted row against the object the flow kept on using.
        private List<Approval> SetupAddedApprovalInsert(Guid createdApprovalId)
        {
            var insertedApprovals = new List<Approval>();

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((Approval approval, CancellationToken cancellationToken) =>
                        {
                            insertedApprovals.Add(approval.DeepClone());

                            Approval storedApproval = approval.DeepClone();
                            storedApproval.Id = createdApprovalId;

                            return new ValueTask<Approval>(storedApproval);
                        });

            return insertedApprovals;
        }

        private List<EventEnvelope<Link>> SetupAddedLinkCommandPublish(
            Action onCommandPublished = null)
        {
            var publishedCommands = new List<EventEnvelope<Link>>();

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkProcessingEventOperation>()))
                        .Returns((EventEnvelope<Link> envelope, LinkEventOperation operation) =>
                        {
                            publishedCommands.Add(envelope);
                            onCommandPublished?.Invoke();

                            return new ValueTask<EventPublishResult<Link>>(
                                new EventPublishResult<Link>());
                        });

            return publishedCommands;
        }

        private void SetupAddedSystemEnvelope<TEntity>() =>
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<TEntity>()))
                    .Returns((TEntity content) =>
                        new ValueTask<EventEnvelope<TEntity>>(
                            new EventEnvelope<TEntity>
                            {
                                Content = content,
                                SecurityContext = new SecurityContext(),
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));
    }
}
