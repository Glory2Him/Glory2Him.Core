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

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddReactionConfigurations(EntityTypeBuilder<Reaction> model)
        {
            model
                .ToTable("Reactions");

            // Primary key
            model
                .HasKey(reaction => reaction.Id);

            // Required properties
            model
                .Property(reaction => reaction.Id)
                .IsRequired();

            model
                .Property(reaction => reaction.Name)
                .HasMaxLength(30)
                .IsRequired();

            model.HasIndex(reaction => reaction.Name)
                .HasDatabaseName("IX_Reactions_Name")
                .IsUnique();

            model
                .Property(reaction => reaction.UnicodeEmoji)
                .HasMaxLength(16)
                .IsRequired();

            // The default matches the entity's own, so a reaction inserted without one lands
            // past the curated seed values rather than at zero, where every unordered reaction
            // would otherwise pile up ahead of the ones somebody chose the order of.
            //
            // ValueGeneratedNever is what lets a SEEDED zero survive (#395). HasDefaultValue on
            // its own marks the property ValueGenerated.OnAdd, and on insert EF omits any value
            // equal to the CLR default - so 0 was the one value the column default overwrote
            // with 1000. The default stays for raw-SQL inserts that name no column; EF now
            // always sends what the entity holds.
            model
                .Property(reaction => reaction.SortOrder)
                .IsRequired()
                .HasDefaultValue(1000)
                .ValueGeneratedNever();

            model
                .Property(reaction => reaction.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(reaction => reaction.CreatedWhen)
                .IsRequired();

            model
                .Property(reaction => reaction.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(reaction => reaction.UpdatedWhen)
                .IsRequired();

            model
                .Property(reaction => reaction.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(reaction => reaction.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(reaction => reaction.DeletedWhen)
                .IsRequired(false);

            model
                .Property(reaction => reaction.DeletionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            model
                .Property(reaction => reaction.PublishDate)
                .IsRequired(false);

            model
                .Property(reaction => reaction.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(reaction => reaction.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(ApprovalStatus.Draft);

            model
                .Property(reaction => reaction.IsApprovedByBypass)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(reaction => reaction.ApprovedByBypassReason)
                .HasMaxLength(500)
                .IsRequired(false);
        }
    }
}
