// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddContentItemConfigurations(EntityTypeBuilder<ContentItem> model)
        {
            model
                .ToTable("ContentItems");

            // Primary key
            model
                .HasKey(contentItem => contentItem.Id);

            // Required properties
            model
                .Property(contentItem => contentItem.Id)
                .IsRequired();

            model
                .Property(contentItem => contentItem.ContentTypeId)
                .IsRequired();

            model.Property(contentItem => contentItem.Content)
                .IsRequired();

            model.Property(contentItem => contentItem.CorrelationId)
                .IsRequired();

            model.Property(contentItem => contentItem.Version)
                .HasDefaultValue(1)
                .IsRequired();

            model
                .Property(contentItem => contentItem.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentItem => contentItem.CreatedWhen)
                .IsRequired();

            model
                .Property(contentItem => contentItem.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentItem => contentItem.UpdatedWhen)
                .IsRequired();

            // Optional properties
            model.Property(contentItem => contentItem.Title);
            model.Property(contentItem => contentItem.Author);

            // Unique constraint on (CorrelationId, Version)
            model.HasIndex(contentItem => new { contentItem.CorrelationId, contentItem.Version })
                .IsUnique();

            // Index on (CorrelationId, Version DESC) for "latest" lookups
            // SQL Server supports DESC index sort order explicitly
            model.HasIndex(contentItem => new { contentItem.CorrelationId, contentItem.Version })
                .HasDatabaseName("IX_ContentItems_CorrelationId_VersionDesc")
                .IsDescending(true, true);

            // Relationship: many ContentItems to one ContentType
            model.HasOne(contentItem => contentItem.ContentType)
                .WithMany(contentType => contentType.ContentItems)
                .HasForeignKey(contentItem => contentItem.ContentTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
