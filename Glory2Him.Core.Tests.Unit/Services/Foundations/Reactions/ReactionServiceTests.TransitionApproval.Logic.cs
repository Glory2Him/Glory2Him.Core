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
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldTransitionReactionApprovalAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Reaction storageReaction = CreateApprovableStorageReaction();
            Reaction inputReaction = CreateApprovalDecision(storageReaction.Id);

            Reaction approvedReaction = storageReaction.DeepClone();
            approvedReaction.ApprovalStatus = inputReaction.ApprovalStatus;
            approvedReaction.IsPublished = inputReaction.IsPublished;
            approvedReaction.PublishDate = inputReaction.PublishDate;
            approvedReaction.IsApprovedByBypass = false;
            approvedReaction.ApprovedByBypassReason = null;

            Reaction auditAppliedReaction = approvedReaction.DeepClone();
            Reaction updatedReaction = auditAppliedReaction.DeepClone();
            Reaction expectedReaction = updatedReaction.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupReactionStorageRead(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(
                    auditAppliedReaction,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction =
                await this.reactionService.TransitionReactionApprovalAsync(
                    inputReaction,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectReactionByIdAsync(
                        inputReaction.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Reaction>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateReactionAsync(
                        auditAppliedReaction,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ReactionOnApprovingReactionSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.AtLeastOnce);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishRejectedWhenTheDecisionRejectsOnApproveAsync()
        {
            // given: the fact follows the DECISION, not the verb. A rejection announced on the
            // Approved address would tell every subscriber the row is live.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            Reaction inputReaction = CreateRejectionDecision(storageReaction.Id);

            // when
            await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Rejected),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Approved),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnApproveAsync()
        {
            // given: the transitions exist to keep the approval workflow's cycle-breaker intact
            // (design §9.7.1). The workflow subscribes to Modified and causes Approved, so an
            // approve that published Modified would re-enter the handler that caused it. This is
            // issue #111 case 1: assert the published operation explicitly, both ways.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            Reaction inputReaction = CreateApprovalDecision(storageReaction.Id);

            // when
            await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishReactionAsync(
                        It.IsAny<EventEnvelope<Reaction>>(),
                        ReactionEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheApprovalFieldsFromTheCallerOnApproveAsync()
        {
            // given: the caller sends a FULLY populated entity whose every non-approval field
            // differs from storage. Approve owns IApproval and nothing else, so the saved row
            // must take the approval values from the caller and everything else from storage
            // (issue #111 case 2: field scope respected). Asserting the whole row against the
            // pre-act snapshot — excluding only the fields approve owns — catches a stray write
            // on ANY other field, without naming entity-specific columns.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            Reaction expectedStorageReaction = storageReaction.DeepClone();

            // a fully random caller copy (differs from storage on every field), pinned only to
            // the id and a valid approval outcome
            Reaction inputReaction = CreateRandomReaction();
            inputReaction.Id = storageReaction.Id;
            inputReaction.ApprovalStatus = ApprovalStatus.Approved;
            inputReaction.IsPublished = true;
            inputReaction.PublishDate = GetRandomDateTimeOffset();

            // when
            Reaction savedReaction = await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            savedReaction.Should().NotBeNull();

            // the fields the operation owns came from the caller
            savedReaction.ApprovalStatus.Should().Be(inputReaction.ApprovalStatus);
            savedReaction.IsPublished.Should().Be(inputReaction.IsPublished);
            savedReaction.PublishDate.Should().Be(inputReaction.PublishDate);

            // everything else came from STORAGE — asserted against the pre-act snapshot, so
            // copying any caller field onto the row fails here. The bypass pair is derived
            // (false / null here) and excluded from the storage comparison.
            savedReaction.Should().BeEquivalentTo(
                expectedStorageReaction,
                options => options
                    .Excluding(reaction => reaction.ApprovalStatus)
                    .Excluding(reaction => reaction.IsPublished)
                    .Excluding(reaction => reaction.PublishDate)
                    .Excluding(reaction => reaction.IsApprovedByBypass)
                    .Excluding(reaction => reaction.ApprovedByBypassReason));
        }

        // ── The bypass record is DERIVED, not copied ─────────────────────────────────────────

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnApproveAsync()
        {
            // given: the caller claims a bypass it was never granted. The decision came back
            // permitted WITHOUT one, so the saved row must say so — otherwise the flag means
            // "the caller said so" rather than "the rules were waived".
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            storageReaction.IsApprovedByBypass = false;
            storageReaction.ApprovedByBypassReason = null;

            Reaction inputReaction = CreateApprovalDecision(storageReaction.Id);
            inputReaction.IsApprovedByBypass = true;
            inputReaction.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            Reaction savedReaction = await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            savedReaction.Should().NotBeNull();
            savedReaction.IsApprovedByBypass.Should().BeFalse();
            savedReaction.ApprovedByBypassReason.Should().BeNull();

            savedReaction.ApprovalStatus.Should().Be(inputReaction.ApprovalStatus);
            savedReaction.IsPublished.Should().Be(inputReaction.IsPublished);
            savedReaction.PublishDate.Should().Be(inputReaction.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            storageReaction.IsApprovedByBypass = false;
            storageReaction.ApprovedByBypassReason = null;

            Reaction inputReaction = CreateApprovalDecision(storageReaction.Id);
            inputReaction.IsApprovedByBypass = false;
            inputReaction.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass();

            // when
            Reaction savedReaction = await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            savedReaction.Should().NotBeNull();
            savedReaction.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. A row that met its conditions this time must stop claiming they were
            // waived, or the flag accumulates for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Reaction storageReaction = CreateApprovableStorageReaction();
            storageReaction.IsApprovedByBypass = true;
            storageReaction.ApprovedByBypassReason = "an earlier bypass";

            Reaction inputReaction = CreateApprovalDecision(storageReaction.Id);

            SetupAccessBrokerToPermit();

            // when
            Reaction savedReaction = await CaptureSavedReactionOnTransitionAsync(storageReaction, inputReaction);

            // then
            savedReaction.Should().NotBeNull();
            savedReaction.IsApprovedByBypass.Should().BeFalse();
            savedReaction.ApprovedByBypassReason.Should().BeNull();
        }
    }
}
