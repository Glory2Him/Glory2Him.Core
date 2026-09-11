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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        /// <summary>
        /// §EVN23. <c>Tag-Submitted</c> is a REQUIRED delivery: it reaches
        /// <c>ApprovalOrchestrationService.OnTagSubmittedAsync</c>, which moves the tag's
        /// approval to Submitted and re-evaluates the round. The substrate CONTAINS a handler
        /// that throws (HandlerFailureContainmentTests, #298), so that failure never reaches the
        /// publisher as an exception — it appears only as <c>IsSuccess == false</c> on the
        /// returned result.
        ///
        /// <para>So a publisher that discards the result loses it entirely, and this one has
        /// nothing to lose it to: §16.7.2's read-triggered repair only opens a MISSING round and
        /// never reconciles an existing Draft round against an entity that has since moved to
        /// Submitted. The divergence would be permanent and invisible.</para>
        ///
        /// <para>The submit itself still SUCCEEDS. The row is committed before the publish, the
        /// caller asked for the write rather than for its fact's onward delivery, and throwing
        /// here would report a committed write as failed — which is the outcome containment
        /// exists to prevent (§EVN23 rule 2).</para>
        /// </summary>
        [Fact]
        public async Task ShouldLogCriticalWhenTheSubmittedFactDeliveryFailsAsync()
        {
            // given
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag submittedTag = storageTag.DeepClone();
            submittedTag.ApprovalStatus = ApprovalStatus.Submitted;

            Tag auditAppliedTag = submittedTag.DeepClone();
            Tag updatedTag = auditAppliedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            Guid failedSubscriptionId = Guid.NewGuid();
            Guid publishedEventId = Guid.NewGuid();

            // the shape HandlerFailureContainmentTests measured: the publish COMPLETED, and the
            // subscriber's failure is reported on the delivery rather than thrown
            var failedPublishResult = new EventPublishResult<Tag>
            {
                EventId = publishedEventId,
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = failedSubscriptionId,
                        IsSuccess = false,
                        IsFailure = true,
                        Status = "Error",
                        ResponseCode = "500",
                        ResponseMessage = "the handler failed",
                    },
                },
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupTagStorageRead(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(failedPublishResult));

            // when
            Tag actualTag =
                await this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken);

            // then: the caller's write stands and is reported as the success it was
            actualTag.Should().BeEquivalentTo(expectedTag);

            // and: that equivalence alone does NOT prove the row was written — the returned
            // object is a clone of the audit-applied one, so a regression that skipped storage
            // and handed back the in-memory transition would satisfy it just as well. The write
            // is asserted on its own, the way the Association delivery test already does.
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and: the contained failure is REPORTED rather than dropped, at the tier this
            // solution reserves for something an operator has to act on
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(
                        FailedEventDeliveryException.ForFailedDeliveries(
                            failedPublishResult,
                            TagEventOperation.Submitted)))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// §EVN23 rule 2 again, at the seam where placement decides whether the rule holds at
        /// all: the outbound dedup write sits between the publish and the report, and that write
        /// CAN fail.
        ///
        /// <para>Reporting after it means a failing <c>InsertProcessedEventAsync</c> unwinds
        /// <c>SaveTransitionAsync</c> before <c>HasFailedDeliveries</c> is ever read — so a
        /// contained subscriber failure goes entirely unreported, which is the exact gap this
        /// section exists to close. The two failures are independent: nothing about the dedup row
        /// failing makes the delivery any less dropped, and the operator needs both.</para>
        ///
        /// <para>The report therefore runs BEFORE the dedup write. It used to run after, so that
        /// a faulting log sink could not cost the outbound event its dedup row — but containment
        /// is what guarantees that now, not ordering, which frees the report to sit where a
        /// bookkeeping failure cannot swallow it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldStillReportTheFailedDeliveryWhenTheDedupWriteFailsAsync()
        {
            // given
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag submittedTag = storageTag.DeepClone();
            submittedTag.ApprovalStatus = ApprovalStatus.Submitted;

            Tag auditAppliedTag = submittedTag.DeepClone();
            Tag updatedTag = auditAppliedTag.DeepClone();

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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupTagStorageRead(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(failedPublishResult));

            // and: the dedup bookkeeping that FOLLOWS the publish fails.
            //
            // The second write, not any write. SaveTransitionAsync records the INBOUND envelope
            // before it publishes and the OUTBOUND one after, so a stub that throws on both
            // faults the transition before PublishTagAsync is ever reached — the report would
            // never run, and this test would be asserting against a path it never took.
            int processedEventWrites = 0;

            this.storageBrokerMock.Setup(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<ProcessedEvent>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((ProcessedEvent processedEvent, CancellationToken _) =>
                        {
                            processedEventWrites++;

                            return processedEventWrites == 1
                                ? new ValueTask<ProcessedEvent>(processedEvent)
                                : throw new Exception("the dedup row could not be written");
                        });

            // when: the transition therefore faults, as it should — the dedup failure is real
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken));

            // then: the publish WAS reached and the outbound write is the one that failed —
            // asserted rather than assumed, because getting this wrong is what would make the
            // verification below vacuous.
            processedEventWrites.Should().Be(2);

            // and: the CONTAINED DELIVERY FAILURE was still reported. Without it, the only
            // record that a required fact was dropped would have been lost to an unrelated
            // bookkeeping fault.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(
                        FailedEventDeliveryException.ForFailedDeliveries(
                            failedPublishResult,
                            TagEventOperation.Submitted)))),
                Times.Once);
        }

        /// <summary>
        /// The other half of the same rule, and the reason the inspection is UNCONDITIONAL: an
        /// address nobody subscribes to returns no deliveries at all, so inspecting it costs
        /// nothing and reports nothing. A publisher therefore never needs a copy of the
        /// subscription list to know whether its fact was required — the result answers it
        /// (§EVN23 rule 1).
        /// </summary>
        [Fact]
        public async Task ShouldNotLogWhenEverySubmittedDeliverySucceedsAsync()
        {
            // given
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag submittedTag = storageTag.DeepClone();
            submittedTag.ApprovalStatus = ApprovalStatus.Submitted;

            Tag auditAppliedTag = submittedTag.DeepClone();
            Tag updatedTag = auditAppliedTag.DeepClone();

            var succeededPublishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = Guid.NewGuid(),
                        IsSuccess = true,
                        Status = "Success",
                    },
                },
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupTagStorageRead(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(succeededPublishResult));

            // when
            await this.tagService.SubmitTagByIdAsync(
                storageTag.Id,
                TestContext.Current.CancellationToken);

            // then
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
