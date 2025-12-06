// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddTagConfigurations(EntityTypeBuilder<Tag> model)
        {
            model
                .ToTable("Tags");

            // Primary key
            model
                .HasKey(tag => tag.Id);

            // Required properties
            model
                .Property(tag => tag.Id)
                .IsRequired();

            model
                .Property(tag => tag.Name)
                .HasMaxLength(30)
                .IsRequired();

            model.HasIndex(tag => tag.Name)
                .HasDatabaseName("IX_Tags_Name")
                .IsUnique();

            model
                .Property(tag => tag.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(tag => tag.CreatedWhen)
                .IsRequired();

            model
                .Property(tag => tag.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(tag => tag.UpdatedWhen)
                .IsRequired();
        }
    }
}
