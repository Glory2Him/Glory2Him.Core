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
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ProcessedEvents
{
    /// <summary>
    /// Proves <c>StorageBroker.SelectProcessedEventExistsAsync</c> against a real catalogue —
    /// the two-part (<c>EventId</c>, <c>ReceiverName</c>) key (issue #637). The receiver-name
    /// CASING behaviour under the catalogue's collation is #639's, not this file's.
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ProcessedEventNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<ProcessedEvent> seededProcessedEvents;

        public ProcessedEventNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededProcessedEvents = new List<ProcessedEvent>();
        }

        [Fact]
        public async Task ShouldReturnTrueOnlyForTheRequestedKeyAsync()
        {
            // given: two distinct (EventId, ReceiverName) rows — the requested pair must reach
            // SQL rather than the probe answering true for any row in the table
            ProcessedEvent requestedProcessedEvent = await SeedProcessedEventAsync();
            await SeedProcessedEventAsync();

            // when
            bool actualExists = await this.broker.StorageBroker.SelectProcessedEventExistsAsync(
                requestedProcessedEvent.EventId,
                requestedProcessedEvent.ReceiverName,
                TestContext.Current.CancellationToken);

            // then
            actualExists.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnFalseForAKeyNobodyHasProcessedAsync()
        {
            // given: nothing seeded — a key nobody has ever recorded

            // when
            bool actualExists = await this.broker.StorageBroker.SelectProcessedEventExistsAsync(
                Guid.NewGuid(), $"Unknown.Receiver.{Guid.NewGuid():N}",
                TestContext.Current.CancellationToken);

            // then: the empty answer, not a fault
            actualExists.Should().BeFalse();
        }

        private async Task<ProcessedEvent> SeedProcessedEventAsync()
        {
            var processedEvent = new ProcessedEvent
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                ReceiverName = $"Seeded.Receiver.{Guid.NewGuid():N}",
                ProcessedAt = DateTimeOffset.UtcNow,
            };

            await this.broker.SeedAsync(processedEvent);
            this.seededProcessedEvents.Add(processedEvent);

            return processedEvent;
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededProcessedEvents).AsTask().GetAwaiter().GetResult();
    }
}
