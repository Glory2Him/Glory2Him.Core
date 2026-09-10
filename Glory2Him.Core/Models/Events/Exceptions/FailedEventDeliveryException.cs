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
using System.Linq;
using Xeptions;

namespace Glory2Him.Core.Models.Events.Exceptions
{
    /// <summary>
    /// A fact was published and at least one subscription failed to receive it (§10.19).
    ///
    /// <para><b>This is never thrown.</b> It exists to carry a message into
    /// <c>ILoggingBroker.LogCriticalAsync</c>, which is the only logging tier that takes an
    /// exception. The write that caused the fact is already committed by the time a publisher
    /// can see this, so raising it would report a committed write as failed — the outcome the
    /// substrate's containment behaviour exists to prevent (§10.19 rule 2).</para>
    ///
    /// <para><b>It carries the substrate's diagnostics and nothing else</b> — the persisted event
    /// id, the address, and each failed subscription's id, status and response. Never the
    /// envelope's content or its <c>SecurityContext</c>: the line exists so a divergence can be
    /// found and repaired, and an event's content in a log is a copy of the row with none of
    /// §14.1's visibility rules attached (§10.19 rule 3).</para>
    /// </summary>
    public class FailedEventDeliveryException : Xeption
    {
        public FailedEventDeliveryException(string message)
            : base(message)
        { }

        /// <summary>
        /// Describes every unsuccessful delivery on a publish result.
        ///
        /// <para>The address is composed from the OPERATION's type rather than the content's,
        /// and that is not interchangeable: <c>ApprovalOrchestrationService</c> publishes
        /// <c>ContentItem</c> content to the PROCESSING address, so naming the address after
        /// the content would name the wrong one in the log.</para>
        /// </summary>
        public static FailedEventDeliveryException ForFailedDeliveries<TContent, TOperation>(
            EventPublishResult<TContent> publishResult,
            TOperation operation)
            where TOperation : struct, Enum
        {
            string address = ComposeAddress(operation);

            string failedDeliveries = string.Join(
                separator: "; ",
                values: publishResult.Deliveries
                    .Where(delivery => delivery.IsSuccess is false)
                    .Select(DescribeDelivery));

            return new FailedEventDeliveryException(
                message: $"Failed event delivery on '{address}' for event " +
                    $"'{publishResult.EventId}'. The publisher completed and its write stands, " +
                    $"but these subscriptions did not receive the fact and nothing redelivers " +
                    $"it: {failedDeliveries}. Contact support.");
        }

        // "TagEventOperation" + Submitted -> "Tag-Submitted"; "ContentItemProcessingEventOperation"
        // + Approving -> "ContentItemProcessing-Approving". The suffix is stripped rather than the
        // subject being passed in, so no call site can name an address its publish did not use.
        private static string ComposeAddress<TOperation>(TOperation operation)
            where TOperation : struct, Enum
        {
            const string operationSuffix = "EventOperation";
            string operationTypeName = typeof(TOperation).Name;

            string subject = operationTypeName.EndsWith(operationSuffix, StringComparison.Ordinal)
                ? operationTypeName[..^operationSuffix.Length]
                : operationTypeName;

            return $"{subject}-{operation}";
        }

        private static string DescribeDelivery<TContent>(EventDelivery<TContent> delivery) =>
            $"subscription '{delivery.SubscriptionId}' reported status '{delivery.Status}'" +
            $" (code '{delivery.ResponseCode}', message '{delivery.ResponseMessage}')";
    }
}
