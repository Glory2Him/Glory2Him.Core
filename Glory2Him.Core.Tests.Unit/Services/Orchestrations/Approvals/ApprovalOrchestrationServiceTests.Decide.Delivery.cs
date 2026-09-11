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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        /// <summary>
        /// §EVN23, on the one publish in the solution that is not merely announcing something.
        /// The approving command is an INSTRUCTION: the Approval row has already been written to
        /// Approved, and the entity only follows because this command reaches it.
        ///
        /// <para>Delivery is contained (HandlerFailureContainmentTests, #298), so a handler that
        /// failed is reported on the delivery and never thrown. A discarded result therefore
        /// leaves the approval saying Approved and the entity still sitting at Submitted — the
        /// §9.8 divergence, permanent and unnoticed, because nothing redelivers the command and
        /// §16.7.2's repair only opens a MISSING round rather than reconciling an existing
        /// one.</para>
        /// </summary>
        [Fact]
        public async Task ShouldLogCriticalWhenTheApprovingCommandDeliveryFailsAsync()
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateDecisionApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Tag);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            SetupDecisionSystemEnvelopes();
            List<Approval> savedApprovals = SetupDecisionApprovalRow(storageApproval);

            var failedPublishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = Guid.NewGuid(),
                        IsSuccess = false,
                        IsFailure = true,
                        Status = "Error",
                        ResponseCode = "500",
                        ResponseMessage = "the handler failed",
                    },
                },
            };

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Approving))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(failedPublishResult));

            // when
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.DecideApprovalAsync(
                    EntityType.Tag,
                    entityId,
                    ApprovalDecision.Approve,
                    false,
                    null,
                    TestContext.Current.CancellationToken);

            // then: the contained failure is reported rather than dropped
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(
                        FailedEventDeliveryException.ForFailedDeliveries(
                            failedPublishResult,
                            TagEventOperation.Approving)))),
                Times.Once);

            // and: the decision itself still stands, asserted on what was WRITTEN rather than on
            // the outcome merely existing. It was recorded before the command went out, and the
            // caller asked for the decision rather than for its delivery — so failing them here
            // would report a committed decision as one that never happened. A test satisfied by
            // a non-null outcome would also pass if the write were skipped entirely while the
            // command and logging paths still ran, which is the regression worth catching.
            Approval decidedApproval = savedApprovals.Should().ContainSingle().Subject;
            decidedApproval.Id.Should().Be(approvalId);
            decidedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.Id == approvalId
                            && approval.ApprovalStatus == ApprovalStatus.Approved),
                    WorkflowAttribution.DecidingCaller,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and: the caller is told the decided status, not a stale one
            actualOutcome.Should().NotBeNull();
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // IsEntitySyncRequested is unchanged by a failed delivery. §16.7.1 defines it as
            // REQUESTED rather than landed, and the command was in fact published — narrowing
            // it to "landed" would be a new contract nobody has ruled on.
            actualOutcome.IsEntitySyncRequested.Should().BeTrue();
        }
    }
}
