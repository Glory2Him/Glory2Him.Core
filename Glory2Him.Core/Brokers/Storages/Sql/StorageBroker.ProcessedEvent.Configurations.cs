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

using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddProcessedEventConfigurations(EntityTypeBuilder<ProcessedEvent> model)
        {
            model
                .ToTable("ProcessedEvents");

            model
                .HasKey(processedEvent => processedEvent.Id);

            model
                .Property(processedEvent => processedEvent.EventId)
                .IsRequired();

            model
                .Property(processedEvent => processedEvent.ReceiverName)
                .IsRequired()
                .HasMaxLength(255);

            model
                .Property(processedEvent => processedEvent.ProcessedAt)
                .IsRequired();

            // The dedup guarantee: the same event can be recorded at most once per receiver.
            model
                .HasIndex(processedEvent => new
                {
                    processedEvent.EventId,
                    processedEvent.ReceiverName
                })
                .IsUnique();
        }
    }
}
