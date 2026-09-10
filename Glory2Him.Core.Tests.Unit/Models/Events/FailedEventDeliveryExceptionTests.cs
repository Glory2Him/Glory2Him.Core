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
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Tests.Unit.Models.Events
{
    /// <summary>
    /// The message §10.19's log line is made of, asserted against LITERALS.
    ///
    /// <para><b>Why this file has to exist separately from the publisher tests.</b> Each
    /// publisher's delivery test builds its expected exception by calling the very factory the
    /// service calls, so the two agree by construction: a regression in the address, in the
    /// per-delivery detail, or in what is deliberately excluded would change both sides at once
    /// and the assertion would still pass. That is fine for proving the publisher REPORTS — it
    /// is worthless for proving WHAT it reports. This file closes that, and it is the only place
    /// the formatting is actually pinned.</para>
    /// </summary>
    public class FailedEventDeliveryExceptionTests
    {
        /// <summary>
        /// The subject is composed from the OPERATION's type, and the composed EVENT NAME is
        /// what the line names — the same name the broker stores the event under (§10.10).
        ///
        /// <para>Deliberately not an "address". <c>HardRemoved</c> is published to the SAME
        /// address as <c>Removed</c> and is distinguished purely by its composed event name, so a
        /// line claiming a <c>Tag-HardRemoved</c> address would name one that does not exist and
        /// send an operator looking for a subscription that was never registered. The event name
        /// is unambiguous for every operation, which the address is not.</para>
        /// </summary>
        [Fact]
        public void ShouldNameTheComposedEventNameForAnOperationSharingAnotherAddress()
        {
            // given
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag> { IsSuccess = false, Status = "Error" },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.HardRemoved);

            // then
            actualException.Message.Should().Contain("TagHardRemoved");
            actualException.Message.Should().NotContain("Tag-HardRemoved");
        }

        /// <summary>
        /// The name comes from the operation's TYPE, never the content's. The orchestration
        /// publishes <c>ContentItem</c> content to the PROCESSING service's address (§12.4.1
        /// rule 10), so naming the event after the content would name the foundation's event and
        /// point an operator at the wrong service entirely.
        /// </summary>
        [Fact]
        public void ShouldNameTheProcessingEventWhenTheContentIsTheFoundationsEntity()
        {
            // given
            var publishResult = new EventPublishResult<ContentItem>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<ContentItem>>
                {
                    new EventDelivery<ContentItem> { IsSuccess = false, Status = "Error" },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    ContentItemProcessingEventOperation.Approving);

            // then
            actualException.Message.Should().Contain("ContentItemProcessingApproving");
            actualException.Message.Should().NotContain("ContentItemApproving");
        }

        /// <summary>
        /// EVERY failed delivery is described, and the successful ones are left out. A line that
        /// named only the first failure would under-report an address with several subscriptions,
        /// which is exactly the case where the operator most needs the full list.
        /// </summary>
        [Fact]
        public void ShouldDescribeEveryFailedDeliveryAndNoSucceededOne()
        {
            // given
            var firstFailedSubscriptionId = Guid.NewGuid();
            var secondFailedSubscriptionId = Guid.NewGuid();
            var succeededSubscriptionId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            var publishResult = new EventPublishResult<Tag>
            {
                EventId = eventId,
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = firstFailedSubscriptionId,
                        IsSuccess = false,
                        Status = "Error",
                        ResponseCode = "500",
                        ResponseMessage = "the first handler failed",
                    },
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = succeededSubscriptionId,
                        IsSuccess = true,
                        Status = "Success",
                    },
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = secondFailedSubscriptionId,
                        IsSuccess = false,
                        Status = "Error",
                        ResponseCode = "503",
                        ResponseMessage = "the second handler failed",
                    },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Submitted);

            // then: the persisted event, so the operator can find the row in the event store
            actualException.Message.Should().Contain(eventId.ToString());

            // and: both failures, each with the substrate's own diagnostics
            actualException.Message.Should().Contain(firstFailedSubscriptionId.ToString());
            actualException.Message.Should().Contain("500");
            actualException.Message.Should().Contain("the first handler failed");

            actualException.Message.Should().Contain(secondFailedSubscriptionId.ToString());
            actualException.Message.Should().Contain("503");
            actualException.Message.Should().Contain("the second handler failed");

            // and: not the delivery that succeeded
            actualException.Message.Should().NotContain(succeededSubscriptionId.ToString());
        }

        /// <summary>
        /// §10.19 rule 3. The line exists so a divergence can be found and repaired; an event's
        /// CONTENT in a log is a copy of the row with none of §14.1's visibility rules attached,
        /// and the caller's identity is worse still. Neither may appear, however convenient it
        /// would be for diagnosis.
        /// </summary>
        [Fact]
        public void ShouldCarryNeitherThePayloadNorTheCallerIdentity()
        {
            // given: a payload AND a caller identity, each carrying text unmistakable in a log.
            // Both sentinels matter: the identity is the one a well-meaning edit is likeliest to
            // append ("which caller's publish failed?"), and with only a Content sentinel this
            // test would pass while the SecurityContext leaked.
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = Guid.NewGuid(),
                        IsSuccess = false,
                        Status = "Error",
                        Response = new EventEnvelope<Tag>
                        {
                            Content = new Tag { Name = "a-secret-tag-name" },
                            SecurityContext = new SecurityContext
                            {
                                SubjectId = "a-secret-caller-id",
                                Username = "a-secret-caller-name",
                            },
                        },
                    },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Submitted);

            // then
            actualException.Message.Should().NotContain("a-secret-tag-name");
            actualException.Message.Should().NotContain("a-secret-caller-id");
            actualException.Message.Should().NotContain("a-secret-caller-name");
        }

        /// <summary>
        /// UNSUCCESSFUL, never undelivered. <c>IsSuccess</c> is set from the listener's own
        /// status, so the commonest failure by far is a subscription that RECEIVED the envelope
        /// and then threw part-way through its own work — the exact case
        /// <c>HandlerFailureContainmentTests</c> measured.
        ///
        /// <para>A line saying the subscription never received the event would send an operator
        /// to the substrate — connectivity, registration, the event store — when the fault is
        /// inside a handler they own. Worth a test of its own because the wrong wording reads
        /// perfectly naturally and would survive review.</para>
        /// </summary>
        [Fact]
        public void ShouldNotClaimASubscriptionNeverReceivedTheEvent()
        {
            // given
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag> { IsSuccess = false, Status = "Error" },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Submitted);

            // then
            actualException.Message.Should().Contain("unsuccessful delivery");
            actualException.Message.Should().NotContain("did not receive");
        }

        /// <summary>
        /// The factory serves instructions as well as facts — the approving COMMAND goes through
        /// the same helper — so the wording stays neutral. A line telling an operator a
        /// subscription "did not receive the fact" about a `-Approving` command describes the
        /// wrong kind of event and sends them looking for a fact that was never published.
        /// </summary>
        [Fact]
        public void ShouldNotCallEveryPublishedEventAFact()
        {
            // given
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag> { IsSuccess = false, Status = "Error" },
                },
            };

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Approving);

            // then
            actualException.Message.Should().NotContain("fact");
        }
    }
}
