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
        /// The WHOLE message, character for character, for the simplest possible case.
        ///
        /// <para>The other tests here assert the parts that carry meaning; this one exists
        /// because they cannot catch a regression in the prefix, the ordering, or the separators
        /// between those parts. An operator greps these lines, so the shape is part of the
        /// contract and not merely presentation.</para>
        ///
        /// <para>It is deliberately the only exact-match assertion in the file. Pinning the full
        /// string in every case would make each of them fail for reasons that have nothing to do
        /// with what they are about, and the first careless fix would be to loosen them all.</para>
        /// </summary>
        [Fact]
        public void ShouldComposeTheWholeMessageInTheDocumentedShape()
        {
            // given
            var eventId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();

            var publishResult = new EventPublishResult<Tag>
            {
                EventId = eventId,
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag>
                    {
                        SubscriptionId = subscriptionId,
                        IsSuccess = false,
                        Status = "Error",
                        ResponseCode = "500",
                        ResponseMessage = "the handler failed",
                    },
                },
            };

            string expectedMessage =
                $"Failed event delivery of 'TagSubmitted', event id '{eventId}'. " +
                $"The publisher completed and its write stands, but these subscriptions " +
                $"reported an unsuccessful delivery and nothing redelivers it: " +
                $"subscription '{subscriptionId}' reported status 'Error' " +
                $"(code '500'). Contact support.";

            // when
            FailedEventDeliveryException actualException =
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Submitted);

            // then
            actualException.Message.Should().Be(expectedMessage);
        }

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

            actualException.Message.Should().Contain(secondFailedSubscriptionId.ToString());
            actualException.Message.Should().Contain("503");

            // and NOT the handlers' own text — see ShouldCarryNeitherThePayloadNorTheCallerIdentity
            actualException.Message.Should().NotContain("the first handler failed");
            actualException.Message.Should().NotContain("the second handler failed");

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
            // given: sentinels in every field that could carry them into the line.
            //
            // ResponseMessage is the one that MATTERS and the one this test originally missed. It
            // is the failed handler's own exception text, copied off the substrate's listener row,
            // and this solution's handlers throw messages like "User {id} does not hold a review
            // role" and "Approval not found for {entityType} with id: {entityId}". Seeding only
            // Response — which the formatter never reads — made the assertion structurally
            // unfailable: it passed while the real leak went straight through ResponseMessage.
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
                        ResponseCode = "500",
                        ResponseMessage =
                            "User a-secret-caller-id does not hold a review role for "
                                + "a-secret-tag-name",
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
        /// A publish with nothing wrong is REFUSED, not rendered.
        ///
        /// <para>Every call site guards on <c>HasFailedDeliveries</c> first, but the invariant
        /// that makes this message meaningful belongs to the type that owns the message, not to
        /// eight call sites and whatever #497 adds. Without the guard the factory renders
        /// "…reported an unsuccessful delivery and nothing redelivers it: . Contact support." —
        /// an operator paged over an empty list, naming nobody.</para>
        /// </summary>
        [Fact]
        public void ShouldRefuseToDescribeAPublishWithNoFailedDelivery()
        {
            // given
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Tag>>
                {
                    new EventDelivery<Tag> { IsSuccess = true, Status = "Success" },
                },
            };

            // when
            Action describingACleanPublish = () =>
                FailedEventDeliveryException.ForFailedDeliveries(
                    publishResult,
                    TagEventOperation.Submitted);

            // then
            describingACleanPublish.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// A null <c>Deliveries</c> reports NO failure rather than throwing.
        ///
        /// <para>It is <c>init</c>-settable, the solution's own integration tests already
        /// null-coalesce it, and this predicate is read AFTER the row is committed — so an
        /// exception here would report a completed write as a failed one, which is the outcome
        /// §10.19 rule 2 exists to prevent.</para>
        /// </summary>
        [Fact]
        public void ShouldReportNoFailureWhenTheDeliveriesAreNull()
        {
            // given
            var publishResult = new EventPublishResult<Tag>
            {
                EventId = Guid.NewGuid(),
                Deliveries = null,
            };

            // when
            bool hasFailedDeliveries = publishResult.HasFailedDeliveries;

            // then
            hasFailedDeliveries.Should().BeFalse();
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
