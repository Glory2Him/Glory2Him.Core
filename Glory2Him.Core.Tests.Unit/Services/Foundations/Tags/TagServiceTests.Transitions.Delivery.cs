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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        /// <summary>
        /// §10.19. <c>Tag-Submitted</c> is a REQUIRED delivery: it reaches
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
        /// exists to prevent (§10.19 rule 2).</para>
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
        /// The other half of the same rule, and the reason the inspection is UNCONDITIONAL: an
        /// address nobody subscribes to returns no deliveries at all, so inspecting it costs
        /// nothing and reports nothing. A publisher therefore never needs a copy of the
        /// subscription list to know whether its fact was required — the result answers it
        /// (§10.19 rule 1).
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
