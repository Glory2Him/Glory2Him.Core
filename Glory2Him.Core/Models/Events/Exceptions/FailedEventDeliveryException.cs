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
using Xeptions;

namespace Glory2Him.Core.Models.Events.Exceptions
{
    /// <summary>
    /// An event was published and at least one subscription reported an unsuccessful delivery
    /// (§EVN23).
    ///
    /// <para>An EVENT rather than a fact, and the distinction is not pedantry: the approving
    /// command travels through this same helper, and telling an operator a subscription failed to
    /// receive a "fact" about a <c>-Approving</c> instruction describes the wrong kind of event
    /// and sends them looking for something nobody published.</para>
    ///
    /// <para><b>UNSUCCESSFUL rather than undelivered</b>, and that is not a hedge either. The
    /// substrate reports Error for a subscription that DID receive the envelope and then threw
    /// part-way through its own work — exactly what <c>HandlerFailureContainmentTests</c>
    /// measured, and the commonest case by far. A line saying the subscription never received it
    /// would point an operator at the substrate when the fault is inside the handler.</para>
    ///
    /// <para>Only Error reaches here. <see cref="EventDelivery{T}.IsFailure"/> is set from the
    /// status enum rather than inverted from success, so the two ordinary transient outcomes —
    /// Pending and Replay — never raise this alarm.</para>
    ///
    /// <para><b>This is never thrown.</b> It exists to carry a message into
    /// <c>ILoggingBroker.LogCriticalAsync</c> — Critical rather than Error because this is a
    /// dependency failure an operator has to act on and the caller is never told about it, which
    /// is the split this solution already uses between the two exception-taking tiers. The write the event announces — or, for a command, the decision it carries —
    /// is already committed by the time a publisher can see this, so raising it would report a
    /// committed write as failed, the outcome the substrate's containment behaviour exists to
    /// prevent (§EVN23 rule 2).</para>
    ///
    /// <para><b>It carries the substrate's diagnostics and nothing else</b> — the persisted event
    /// id, the composed event name, and each failed subscription's id, status and response CODE.
    /// Never the handler's own response MESSAGE — that is its exception text, and the messages in
    /// this solution carry entity ids and caller identities. Never an address either: <c>HardRemoved</c> shares <c>Removed</c>'s, so only the name discriminates.
    /// And never the
    /// envelope's content or its <c>SecurityContext</c>: the line exists so a divergence can be
    /// found and repaired, and an event's content in a log is a copy of the row with none of
    /// §14.1's visibility rules attached (§EVN23 rule 3).</para>
    /// </summary>
    public class FailedEventDeliveryException : Xeption
    {
        private FailedEventDeliveryException(string message)
            : base(message)
        { }

        /// <summary>
        /// Describes every unsuccessful delivery on a publish result.
        ///
        /// <para>The event name is composed from the OPERATION's type rather than the
        /// content's, and that is not interchangeable: <c>ApprovalOrchestrationService</c>
        /// publishes <c>ContentItem</c> content to the PROCESSING service's address, so naming
        /// the event after the content would point an operator at the wrong service.</para>
        /// </summary>
        public static FailedEventDeliveryException ForFailedDeliveries<TContent, TOperation>(
            EventPublishResult<TContent> publishResult,
            TOperation operation)
            where TOperation : struct, Enum
        {
            string eventName = ComposeEventName(operation);
            IReadOnlyList<EventDelivery<TContent>> failures = publishResult.FailedDeliveries;

            // Refuses rather than renders. Every caller guards on HasFailedDeliveries first, but
            // the invariant that makes this message meaningful belongs to the type that owns the
            // message — not to eight call sites. Without this, a result with nothing wrong
            // produces "...reported an unsuccessful delivery ... : . Contact support.": an
            // operator paged over an empty list, naming nobody.
            if (failures.Count == 0)
            {
                throw new InvalidOperationException(
                    "FailedEventDeliveryException.ForFailedDeliveries was called for a publish " +
                    "with no failed delivery. Guard on EventPublishResult.HasFailedDeliveries.");
            }

            string failedDeliveries = string.Join(
                separator: "; ",
                values: failures.Select(DescribeDelivery));

            return new FailedEventDeliveryException(
                message: $"Failed event delivery of '{eventName}', event id " +
                    $"'{publishResult.EventId}'. The publisher completed and its write stands, " +
                    $"but these subscriptions reported an unsuccessful delivery and nothing " +
                    $"redelivers it: {failedDeliveries}. Contact support.");
        }

        // "TagEventOperation" + Submitted -> "TagSubmitted"; "ContentItemProcessingEventOperation"
        // + Approving -> "ContentItemProcessingApproving". The suffix is stripped rather than the
        // subject being passed in, so no call site can name an event its publish did not send.
        //
        // The composed EVENT NAME rather than the event address, and they are not
        // interchangeable: HardRemoved is published to the same address as Removed and is
        // distinguished purely by this name (see TagEventOperation), so an address composed this
        // way would announce a "Tag-HardRemoved" that was never registered and send an operator
        // hunting a subscription that does not exist. The event name is unambiguous for every
        // operation, and it is what the event store holds the row under (§10.10).
        private static string ComposeEventName<TOperation>(TOperation operation)
            where TOperation : struct, Enum
        {
            const string operationSuffix = "EventOperation";
            string operationTypeName = typeof(TOperation).Name;

            string subject = operationTypeName.EndsWith(operationSuffix, StringComparison.Ordinal)
                ? operationTypeName[..^operationSuffix.Length]
                : operationTypeName;

            return $"{subject}{operation}";
        }

        // The subscription, the status and the response CODE — never ResponseMessage.
        //
        // That field is the failed handler's own exception text, copied verbatim off the
        // substrate's listener row, and this solution's handler messages routinely carry exactly
        // what §EVN23 rule 3 excludes: entity ids, and caller identities like "User {id} does not
        // hold a review role". Rendering it here would have put payload and identity into a
        // Critical line through the one field nobody thought to look at, and would have made the
        // line unbounded — N failed subscriptions x arbitrary handler text.
        //
        // The operator loses nothing they need: the event id and subscription id below locate the
        // delivery row, which holds the full message under the event store's own access rules.
        private static string DescribeDelivery<TContent>(EventDelivery<TContent> delivery) =>
            $"subscription '{delivery.SubscriptionId}' reported status '{delivery.Status}'" +
            $" (code '{delivery.ResponseCode}')";
    }
}
