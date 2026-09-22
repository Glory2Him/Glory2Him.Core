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
    internal partial class StorageBroker
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

            // A receiver name is an IDENTIFIER, and identifiers compare exactly: a name
            // differing only in case is a DIFFERENT receiver. These names are public const
            // members of EventBrokerIdentifiers.*, so they name a C# member path, and C# is
            // case-sensitive.
            //
            // The collation is pinned on the COLUMN rather than on an expression, because the
            // probe SelectProcessedEventExistsAsync and the unique index below must agree and
            // the column is the only thing both inherit. If they ever disagreed, the probe
            // would answer "not processed", the handler would run its side effect, and the
            // insert would THEN violate the index — a failure AFTER the effect, which is the
            // one outcome worse than either semantics.
            //
            // Without this the column inherits the catalogue's collation, which defaults to
            // case-insensitive, so today's behaviour is correct only by accident and a restore
            // onto a differently-collated server would change it with nothing announcing it.
            // An expression COLLATE — the instrument CK_Association_CanonicalOrder uses — is
            // wrong here: it cannot be expressed in the LINQ predicate portably, and in an
            // index key it would make the index a computed one. Normalising on write would put
            // the rule in every writer and every reader instead of in one place.
            model
                .Property(processedEvent => processedEvent.ReceiverName)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("Latin1_General_BIN2");

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
