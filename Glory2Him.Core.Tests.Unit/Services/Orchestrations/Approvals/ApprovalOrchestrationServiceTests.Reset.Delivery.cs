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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        /// <summary>
        /// §EVN23 rule 2, on the seam where the rule is hardest to satisfy: the delivery report
        /// is a shared helper, so being the last statement of <c>PublishCommandAsync</c> is NOT
        /// the same as being the last thing the OPERATION does.
        ///
        /// <para><c>ResetApprovalAsync</c> runs the entity sync and then
        /// <c>ResetStaleAIReviewerAssignmentAsync</c> — §8.6.2's flag reset, which is owed work
        /// and runs AFTER the publish. <c>LoggingBroker.LogCriticalAsync</c> has no try/catch of
        /// its own, so a faulting sink (back-pressure, a disposed provider at shutdown) would
        /// propagate out of the report, out of the sync, and skip that reset entirely — leaving
        /// Berean's two flags claiming a completed pass over content the override has just put
        /// back for review.</para>
        ///
        /// <para>It would also surface a reset that FULLY SUCCEEDED as a failure: by the time the
        /// report runs the status is Submitted, the reviews are dismissed and the entity is
        /// unpublished, and none of it can be taken back. The moderator would be told to try
        /// again, and the retry is then refused by <c>ValidateStorageApprovalIsDecided</c>
        /// because the round is no longer decided — a second, unrelated error on a round that
        /// was never broken. <c>Resets.cs</c> states that outcome as the thing its ordering
        /// exists to prevent; a throwing report reintroduces it from underneath.</para>
        ///
        /// <para>So the report is contained the same way
        /// <c>ResetStaleAIReviewerAssignmentAsync</c> and Substrate's <c>onVerified</c> hook are:
        /// bookkeeping on somebody else's path does not get to decide that path's outcome.</para>
        /// </summary>
        [Fact]
        public async Task ShouldStillResetTheStaleAIReviewerAssignmentWhenTheDeliveryReportThrowsAsync()
        {
            // given: an administrator takes back an approved round
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Approval decidedApproval = SetupDecidedRound(ApprovalStatus.Approved);
            SetupEntityVisibility(isEntityVisible: true);

            // and: the entity sync reports a contained delivery failure, so the §EVN23 report runs
            var failedPublishResult = new EventPublishResult<ContentItem>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<ContentItem>>
                {
                    new EventDelivery<ContentItem>
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
                broker.PublishContentItemProcessingAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemProcessingEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            failedPublishResult));

            // and: the sink that report goes to is down
            this.loggingBrokerMock.Setup(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()))
                    .Throws(new Exception("the logging sink is down"));

            var resettableAssignmentId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(resettableAssignmentId);

            // when: the reset is asked for
            ApprovalOutcome actualOutcome =
                await this.approvalOrchestrationService.ResetApprovalAsync(
                    entityType: decidedApproval.EntityType,
                    entityId: decidedApproval.EntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: the owed flag reset STILL RUNS. This is the assertion the whole test exists
            // for — a report that throws would have unwound before this line was ever reached.
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    resettableAssignmentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and: the caller is told the reset worked, because it did
            actualOutcome.Should().NotBeNull();
            actualOutcome.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
        }
    }
}
