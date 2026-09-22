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
    /// Proves that a receiver name is an IDENTIFIER — it compares exactly, and a name differing
    /// only in case names a DIFFERENT receiver.
    ///
    /// <para><b>Why the database and not the service.</b> Two things decide this and neither is
    /// C#: the probe <c>SelectProcessedEventExistsAsync</c>, whose predicate SQL Server evaluates
    /// under the column's collation, and the unique index on (EventId, ReceiverName), which is
    /// keyed under that same collation. Nothing above the broker can tell a case-insensitive
    /// column from a binary one — LINQ-to-objects would answer ordinally whatever the catalogue
    /// does — so the question is only answerable against a real catalogue.</para>
    ///
    /// <para><b>Why the two must agree.</b> If they ever disagreed, the probe would answer "not
    /// processed", the handler would run its side effect, and the insert would THEN violate the
    /// index — a failure after the effect, which is worse than either semantics. Pinning the
    /// COLUMN's collation is what makes them agree, because both inherit it.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ProcessedEventReceiverNameCollationTests : IAsyncDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<ProcessedEvent> seededProcessedEvents;

        public ProcessedEventReceiverNameCollationTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededProcessedEvents = new List<ProcessedEvent>();
        }

        [Fact]
        public async Task ShouldTreatADifferentlyCasedReceiverNameAsADifferentReceiverAsync()
        {
            // given: one event recorded under one casing of a receiver name, and the same name
            // differing ONLY in case — the shape two constants that differed only in case would
            // arrive in
            Guid eventId = Guid.NewGuid();
            string receiverName = CreateRandomReceiverName();
            string differentlyCasedReceiverName = receiverName.ToUpperInvariant();

            ProcessedEvent recordedProcessedEvent =
                await SeedProcessedEventAsync(eventId, receiverName);

            // when
            bool exactNameWasProcessed =
                await this.broker.StorageBroker.SelectProcessedEventExistsAsync(
                    eventId, receiverName, CancellationToken.None);

            bool differentlyCasedNameWasProcessed =
                await this.broker.StorageBroker.SelectProcessedEventExistsAsync(
                    eventId, differentlyCasedReceiverName, CancellationToken.None);

            // then: the exact name answers true first, so a false below cannot be the seed
            // having failed to land — the two answers are what separate "different receiver"
            // from "no row at all"
            exactNameWasProcessed.Should().BeTrue(
                because: "the row was recorded under exactly this receiver name");

            differentlyCasedNameWasProcessed.Should().BeFalse(
                because: "a receiver name is an identifier and identifiers compare exactly, so "
                    + "a name differing only in case is a different receiver that has not "
                    + "processed this event");

            recordedProcessedEvent.ReceiverName.Should().Be(receiverName,
                because: "nothing normalises the name on write — the column's collation is what "
                    + "decides, not the writer");
        }

        private static string CreateRandomReceiverName() =>
            $"ReceiverCollationProbe_{Guid.NewGuid():N}";

        private async ValueTask<ProcessedEvent> SeedProcessedEventAsync(
            Guid eventId,
            string receiverName)
        {
            var processedEvent = new ProcessedEvent
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ReceiverName = receiverName,
                ProcessedAt = DateTimeOffset.UtcNow
            };

            this.seededProcessedEvents.Add(processedEvent);
            await this.broker.SeedAsync(processedEvent);

            return processedEvent;
        }

        public async ValueTask DisposeAsync() =>
            await this.broker.ClearAsync(this.seededProcessedEvents);
    }
}
