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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Registrations
{
    /// <summary>
    /// Measures ONE property of the substrate: a delivery runs on the PUBLISHER'S execution
    /// context, so an <c>AsyncLocal</c> set before a publish is readable inside the handler
    /// that publish reaches.
    ///
    /// <para><b>Why it has to be measured rather than reasoned about.</b> Delivery being
    /// synchronous is not the same claim as delivery running on the publisher's context — a
    /// substrate could await a queue, hop a thread, or suppress the flow, and every one of
    /// those is synchronous from the publisher's point of view while carrying nothing across.
    /// Nothing in the shape of the calling code distinguishes them.</para>
    ///
    /// <para><b>Two places in production depend on the answer</b>, which is why this file
    /// exists at all rather than being folded into whichever test needed it first:</para>
    /// <list type="number">
    /// <item><c>ApprovalOrchestrationService.Flows.cs</c>'
    /// <c>suppressedDismissalApprovalId</c> — the §9.7.4 dismissal loop announces the round it
    /// is tearing down, and the re-entered <c>ApprovalReview-Dismissed</c> handler reads that
    /// announcement to stand down. If the value did not flow, the guard would be dead code and
    /// the workflow would re-test a round mid-teardown
    /// (<c>ApprovalOrchestrationServiceTests.DismissalReEntrancy.cs</c> holds the behaviour
    /// that rests on it).</item>
    /// <item><c>IContentItemService.cs</c>'s internal envelope-taking read (§12.5.2 rule 6,
    /// #456) — the OPPOSITE direction of the same property. There the flow is a hazard rather
    /// than a mechanism: <c>HttpContextAccessor</c> is itself an <c>AsyncLocal</c>, so an
    /// event-path read that minted its own envelope would inherit whoever PUBLISHED, who for a
    /// relayed or system-minted envelope is not the subject the envelope was signed for. That
    /// overload exists precisely because identity DOES flow and must therefore be carried
    /// explicitly instead.</item>
    /// </list>
    ///
    /// <para>Delete this file and both of those become assumptions nothing checks.</para>
    /// </summary>
    [Collection(EventSubstrateCollection.Name)]
    public sealed class ExecutionContextFlowTests
    {
        // Static for the same reason the production guard is: the handler is bound as a method
        // group and runs wherever the substrate calls it, so the value cannot live on an
        // instance the delivery has no way to reach.
        private static readonly AsyncLocal<Guid> publisherScopedValue = new();

        private readonly EventSubstrateBroker broker;

        public ExecutionContextFlowTests(EventSubstrateBroker broker) =>
            this.broker = broker;

        [Fact]
        public async Task ShouldFlowThePublishersAsyncLocalIntoTheDeliveredHandlerAsync()
        {
            // given: a probe bound to a real address, and a value announced only AFTER it was
            // bound. The ordering matters: subscriptions are registered once at startup, long
            // before any request sets anything, so a substrate that captured the context at
            // SUBSCRIBE time would carry nothing and this would fail — which is the correct
            // answer for the production callers, not a defect in the test.
            Guid announcedValue = Guid.NewGuid();
            var observedValues = new List<Guid>();

            await this.broker.EventBroker.SubscribeToApprovalReviewEventAsync(
                subscription: new EventSubscription
                {
                    Id = Guid.NewGuid(),
                    Name = $"IntegrationProbe.ExecutionContextFlow.{Guid.NewGuid():N}",

                    Description = "Reads an AsyncLocal the publisher set, from inside a " +
                        "delivery."
                },
                operation: ApprovalReviewEventOperation.Dismissed,
                approvalReviewEventHandler: (EventEnvelope<ApprovalReview> _,
                    CancellationToken __) =>
                {
                    observedValues.Add(publisherScopedValue.Value);

                    return ValueTask.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            publisherScopedValue.Value = announcedValue;

            try
            {
                // when
                EventPublishResult<ApprovalReview> publishResult =
                    await this.broker.EventBroker.PublishApprovalReviewAsync(
                        new EventEnvelope<ApprovalReview>
                        {
                            Content = new ApprovalReview
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = Guid.NewGuid()
                            }
                        },
                        ApprovalReviewEventOperation.Dismissed);

                // then: the probe was reached at all — without this, an empty observation list
                // would read as "the value did not flow" when it actually means "nothing was
                // delivered".
                publishResult.Should().NotBeNull();

                observedValues.Should().ContainSingle(
                    because: "the probe is bound to the address the fact was published to, and " +
                        "a publish that reached nothing would make every assertion below " +
                        "vacuous");

                observedValues[0].Should().Be(announcedValue,
                    because: "delivery runs on the PUBLISHER's execution context, which is the " +
                        "whole mechanism the dismissal re-entrancy guard uses to recognise its " +
                        "own work — and the same mechanism §12.5.2 rule 6 has to defend " +
                        "against on the identity path");
            }
            finally
            {
                // Restored rather than left standing, for the reason the production loop uses
                // try/finally: this value is read by whatever runs next on this context, and a
                // leaked announcement is indistinguishable from a real one.
                publisherScopedValue.Value = Guid.Empty;
            }
        }
    }
}
