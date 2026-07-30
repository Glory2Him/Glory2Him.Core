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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

        public async ValueTask<ProcessedEvent> InsertProcessedEventAsync(
            ProcessedEvent processedEvent,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(processedEvent, cancellationToken);

        public async ValueTask<bool> SelectProcessedEventExistsAsync(
            Guid eventId,
            string receiverName,
            CancellationToken cancellationToken = default) =>
            await this.ProcessedEvents.AnyAsync(
                processedEvent =>
                    processedEvent.EventId == eventId
                        && processedEvent.ReceiverName == receiverName,
                cancellationToken);
    }
}
