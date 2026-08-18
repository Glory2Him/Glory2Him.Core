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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Theory]
        [InlineData(ApprovalDecision.Approve, ApprovalStatus.Approved)]
        [InlineData(ApprovalDecision.Reject, ApprovalStatus.Rejected)]
        public async Task ShouldWriteTheDecidedStatusOntoTheApprovalRowAsync(
            ApprovalDecision decision,
            ApprovalStatus expectedApprovalStatus)
        {
            // given: the row starts SUBMITTED, so the decided status is a change rather than
            // something already there. The saved row is captured as a SNAPSHOT taken inside the
            // save — the service mutates the very object it retrieved and hands that same instance
            // on, so asserting against the object this test supplied would compare it with itself
            // and pass whatever the service did to it.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionLinkCommandPublish();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                decision,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then
            savedApprovals.Should().ContainSingle();
            Approval savedApproval = savedApprovals.Single();
            savedApproval.ApprovalStatus.Should().Be(expectedApprovalStatus);

            // The row written is the one the PROBE found, addressed by the approval's own id. The
            // entity id is a different value on purpose — a read that went looking by the entity
            // id would be invisible if the two shared a variable.
            savedApproval.Id.Should().Be(approvalId);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldWriteTheApprovalRowBeforePublishingTheEntityCommandAsync()
        {
            // given: §9.8 names Approval.ApprovalStatus the source of truth, so it is written
            // FIRST and the entity follows. Entity-first would make a repair pass — which can only
            // mean "drive the entity to match the approval" — revert a decision that really
            // happened. Both sides stamp a shared counter, so the order is observed rather than
            // assumed from the two calls merely having occurred.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            int decisionStep = 0;
            int approvalRowWrittenAt = 0;
            int entityCommandPublishedAt = 0;

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();

            SetupDecisionApprovalRow(
                storageApproval,
                onApprovalSaved: () => approvalRowWrittenAt = ++decisionStep);

            SetupDecisionLinkCommandPublish(
                onCommandPublished: () => entityCommandPublishedAt = ++decisionStep);

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                ApprovalDecision.Approve,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then
            approvalRowWrittenAt.Should().Be(1);
            entityCommandPublishedAt.Should().Be(2);
        }

        [Fact]
        public async Task ShouldRecordNoBypassWhenTheVerdictDidNotUseTheOneRequestedAsync()
        {
            // given: the caller asks for a bypass and the decision function answers that it did
            // not need one. The pair is taken from the VERDICT, never from the request —
            // otherwise "what was approved without meeting its conditions" answers with rows that
            // met them (§9.7.1 rule 3). The stored row arrives already carrying a stale bypass, so
            // leaving the field untouched is a failure rather than an accidental pass.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            string requestedBypassReason = GetRandomString();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                isApprovedByBypass: true,
                approvedByBypassReason: "stale bypass reason");

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            // Both answers say the conditions were met on their own merits.
            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);

            List<EventEnvelope<Link>> publishedCommands =
                SetupDecisionLinkCommandPublish();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Link,
                    entityId,
                    ApprovalDecision.Approve,
                    true,
                    requestedBypassReason,
                    TestContext.Current.CancellationToken);

            // then
            Approval savedApproval = savedApprovals.Single();
            savedApproval.IsApprovedByBypass.Should().BeFalse();
            savedApproval.ApprovedByBypassReason.Should().BeNull();

            // The reason must be CLEARED alongside the flag. A row reading "not bypassed" beside
            // a bypass reason is the record contradicting itself.
            savedApproval.ApprovedByBypassReason.Should().NotBe(requestedBypassReason);
            savedApproval.ApprovedByBypassReason.Should().NotBe("stale bypass reason");

            Link publishedLink = publishedCommands.Single().Content;
            publishedLink.IsApprovedByBypass.Should().BeFalse();
            publishedLink.ApprovedByBypassReason.Should().BeNull();

            actualOutcome.IsApprovedByBypass.Should().BeFalse();
            actualOutcome.ApprovedByBypassReason.Should().BeNull();

            // and the request still reached the decision — the waiver is refused a RECORD, not
            // silently dropped before the question that decides whether it was needed.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    approvalId,
                    ApprovalDecision.Approve,
                    true,
                    requestedBypassReason,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRecordTheBypassAndTheSuppliedReasonWhenTheVerdictUsedOneAsync()
        {
            // given: the inverse — the decision function answers that it DID waive the §8.5
            // conditions. The row starts with no bypass on it, so both fields moving is what is
            // observed, and a bypass is only tolerable because it leaves a record, so the reason
            // travels with the flag.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            string requestedBypassReason = GetRandomString();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                isApprovedByBypass: false,
                approvedByBypassReason: null);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: CreateDecisionBypassUsedVerdict());

            SetupDecisionSystemEnvelopes();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);

            List<EventEnvelope<Link>> publishedCommands =
                SetupDecisionLinkCommandPublish();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Link,
                    entityId,
                    ApprovalDecision.Approve,
                    true,
                    requestedBypassReason,
                    TestContext.Current.CancellationToken);

            // then
            Approval savedApproval = savedApprovals.Single();
            savedApproval.IsApprovedByBypass.Should().BeTrue();
            savedApproval.ApprovedByBypassReason.Should().Be(requestedBypassReason);

            // The entity carries the same pair, because "what was published without meeting its
            // conditions" is meant to be a query rather than a join.
            Link publishedLink = publishedCommands.Single().Content;
            publishedLink.IsApprovedByBypass.Should().BeTrue();
            publishedLink.ApprovedByBypassReason.Should().Be(requestedBypassReason);

            actualOutcome.IsApprovedByBypass.Should().BeTrue();
            actualOutcome.ApprovedByBypassReason.Should().Be(requestedBypassReason);
        }

        [Fact]
        public async Task ShouldMintTheCommandUnderTheSystemIdentityAndDecideUnderTheCallersAsync()
        {
            // given: two identities, one flow. The authorisation question is asked under the
            // CALLER captured by the ordinary envelope, and the entity command is published under
            // the WORKFLOW's — asking again on the command path would fail deterministically,
            // since the decision function refuses any outcome once the approval is no longer
            // Submitted, which by then it is not (§16.7.1).
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            SecurityContext callerSecurityContext = this.ambientSecurityContext;

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionApprovalRow(storageApproval);
            SetupDecisionLinkCommandPublish();

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                ApprovalDecision.Approve,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then: the entity command's envelope is the SYSTEM one, and the caller-identity
            // minting is never used for it.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Link>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Link>()),
                Times.Never);

            // The caller-identity envelope names the entity being decided, and is the ONLY one
            // minted for the Approval — a system envelope here would ask the policy about the
            // workflow rather than about the person.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is<Approval>(approval =>
                    approval.EntityType == EntityType.Link
                        && approval.EntityId == entityId)),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<Approval>()),
                Times.Never);

            // and the ONE authorisation was asked with the caller's own context.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    approvalId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    callerSecurityContext,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // asked ONCE. The verdict read asks twice on purpose; the decision must not, or a
            // caller refused the plain approve would be quietly re-asked with a bypass.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldCarryTheEntityIdentityAndDecidedStateOnThePublishedCommandAsync()
        {
            // given: the approval's id and the entity's id are DIFFERENT values. The command
            // addresses the ENTITY — a payload keyed by the approval id would name a row the
            // receiving transition has never heard of, and the mistake is invisible when the two
            // ids share a variable.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            string requestedBypassReason = GetRandomString();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: CreateDecisionBypassUsedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionApprovalRow(storageApproval);

            List<EventEnvelope<Link>> publishedCommands =
                SetupDecisionLinkCommandPublish();

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                ApprovalDecision.Approve,
                true,
                requestedBypassReason,
                TestContext.Current.CancellationToken);

            // then
            publishedCommands.Should().ContainSingle();
            Link publishedLink = publishedCommands.Single().Content;

            publishedLink.Id.Should().Be(entityId);
            publishedLink.Id.Should().NotBe(approvalId);
            publishedLink.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            publishedLink.IsApprovedByBypass.Should().BeTrue();
            publishedLink.ApprovedByBypassReason.Should().Be(requestedBypassReason);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkProcessingEventOperation.Approving),
                Times.Once);
        }

        [Theory]
        [InlineData(ApprovalDecision.Approve, ApprovalStatus.Approved, true)]
        [InlineData(ApprovalDecision.Reject, ApprovalStatus.Rejected, false)]
        public async Task ShouldPublishTheEntityOnlyWhenTheDecisionApprovesItAsync(
            ApprovalDecision decision,
            ApprovalStatus expectedApprovalStatus,
            bool expectedIsPublished)
        {
            // given: publication is asked for only alongside an APPROVAL. A rejection that left
            // IsPublished set — or a publish date behind a false flag — would leave the entity
            // readable by every query that filters on publication while the approval says it was
            // refused.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionApprovalRow(storageApproval);

            List<EventEnvelope<Link>> publishedCommands =
                SetupDecisionLinkCommandPublish();

            DateTimeOffset beforeDecision = DateTimeOffset.UtcNow;

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                EntityType.Link,
                entityId,
                decision,
                false,
                null,
                TestContext.Current.CancellationToken);

            DateTimeOffset afterDecision = DateTimeOffset.UtcNow;

            // then
            Link publishedLink = publishedCommands.Single().Content;
            publishedLink.ApprovalStatus.Should().Be(expectedApprovalStatus);
            publishedLink.IsPublished.Should().Be(expectedIsPublished);

            if (expectedIsPublished)
            {
                // Stamped at the moment of the decision, so the date is bounded by the act rather
                // than merely being non-null — a hard-coded or defaulted date would satisfy the
                // weaker assertion.
                publishedLink.PublishDate.Should().NotBeNull();

                publishedLink.PublishDate.Value
                    .Should().BeOnOrAfter(beforeDecision)
                    .And.BeOnOrBefore(afterDecision);
            }
            else
            {
                publishedLink.PublishDate.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Reaction)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Association)]
        public async Task ShouldPublishTheApprovingCommandOnTheDecidedEntitysOwnChannelAsync(
            EntityType entityType)
        {
            // given: each approvable type owns a channel, and the decision must reach THAT one
            // under the Approving operation. A command delivered on the wrong channel — or under
            // a neighbouring operation — decides the Approval row and leaves its entity behind,
            // diverging the two records §9.8 requires never to diverge.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: entityType);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionApprovalRow(storageApproval);

            // when
            await this.approvalOrchestrationService.DecideApprovalAsync(
                entityType,
                entityId,
                ApprovalDecision.Approve,
                false,
                null,
                TestContext.Current.CancellationToken);

            // then
            switch (entityType)
            {
                case EntityType.Tag:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishTagAsync(
                            It.Is<EventEnvelope<Tag>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            TagEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.ContentItem:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishContentItemProcessingAsync(
                            It.Is<EventEnvelope<ContentItem>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            ContentItemProcessingEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.Link:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishLinkProcessingAsync(
                            It.Is<EventEnvelope<Link>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            LinkProcessingEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.Comment:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishCommentAsync(
                            It.Is<EventEnvelope<Comment>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            CommentEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.Reaction:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishReactionAsync(
                            It.Is<EventEnvelope<Reaction>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            ReactionEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.BibleReference:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishBibleReferenceAsync(
                            It.Is<EventEnvelope<BibleReference>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            BibleReferenceEventOperation.Approving),
                        Times.Once);
                    break;

                case EntityType.Association:
                    this.eventBrokerMock.Verify(broker =>
                        broker.PublishAssociationAsync(
                            It.Is<EventEnvelope<Association>>(envelope =>
                                envelope.Content.Id == entityId
                                    && envelope.Content.ApprovalStatus == ApprovalStatus.Approved),
                            AssociationEventOperation.Approving),
                        Times.Once);
                    break;
            }

            // and no OTHER channel heard about it. One decision must not fan out across types —
            // a sibling entity carrying an id it does not own is a decision applied to a row
            // nobody decided.
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnTheOutcomeTakenFromTheDecidedApprovalRowAsync()
        {
            // given: the approval id, the entity id and the entity type are pinned to values that
            // cannot be confused for one another — two distinct guids, and a type that is not the
            // enum's zero member. An outcome that echoed the wrong field would otherwise satisfy
            // assertions written against a value both fields happen to hold.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            EntityType entityType = EntityType.Link;
            string requestedBypassReason = GetRandomString();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: entityType);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: CreateDecisionBypassUsedVerdict());

            SetupDecisionSystemEnvelopes();
            SetupDecisionApprovalRow(storageApproval);
            SetupDecisionLinkCommandPublish();

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    entityType,
                    entityId,
                    ApprovalDecision.Approve,
                    true,
                    requestedBypassReason,
                    TestContext.Current.CancellationToken);

            // then
            actualOutcome.ApprovalId.Should().Be(approvalId);
            actualOutcome.EntityId.Should().Be(entityId);
            actualOutcome.EntityType.Should().Be(entityType);
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualOutcome.IsApprovedByBypass.Should().BeTrue();
            actualOutcome.ApprovedByBypassReason.Should().Be(requestedBypassReason);

            // REQUESTED, not confirmed. The command travels as an event, so §9.8's "must never
            // diverge" is a steady-state invariant rather than a claim that both rows were
            // written in one instant (§16.7.1).
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
        }

        private static Approval CreateDecisionApproval(
            Guid approvalId,
            Guid entityId,
            EntityType entityType,
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted,
            bool isApprovedByBypass = false,
            string approvedByBypassReason = null) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = isApprovedByBypass,
                ApprovedByBypassReason = approvedByBypassReason,
            };

        // A permitted verdict that WAIVED the §8.5 conditions, carrying what would otherwise have
        // blocked it — the difference between a bypass worth investigating and a harmless one.
        private static AccessVerdict CreateDecisionBypassUsedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = true,
                BypassedBlockReason = AccessDenialReason.ApprovalThresholdNotMet,
                Explanation = GetRandomString(),
            };

        // The save is captured as a SNAPSHOT rather than as the instance handed to it. The service
        // mutates the row it retrieved and passes that same object on, so a test holding the
        // original would be reading whatever the service wrote into it and asserting against
        // itself. The clone is also what travels onward, matching a real store returning its own
        // copy of the row.
        private List<Approval> SetupDecisionApprovalRow(
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

        private List<EventEnvelope<Link>> SetupDecisionLinkCommandPublish(
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

        // Every command-carrying type, so a dispatch test never passes merely because the envelope
        // for the type under test was the only one a mock could produce.
        private void SetupDecisionSystemEnvelopes()
        {
            SetupDecisionSystemEnvelope<Tag>();
            SetupDecisionSystemEnvelope<ContentItem>();
            SetupDecisionSystemEnvelope<Link>();
            SetupDecisionSystemEnvelope<Comment>();
            SetupDecisionSystemEnvelope<Reaction>();
            SetupDecisionSystemEnvelope<BibleReference>();
            SetupDecisionSystemEnvelope<Association>();
        }

        private void SetupDecisionSystemEnvelope<TEntity>() =>
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
