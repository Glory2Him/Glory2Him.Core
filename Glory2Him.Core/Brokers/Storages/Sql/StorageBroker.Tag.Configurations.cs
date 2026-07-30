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
using Glory2Him.Core.Models.Foundations.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
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

            model
                .Property(tag => tag.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(tag => tag.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(tag => tag.DeletedWhen)
                .IsRequired(false);

            model
                .Property(tag => tag.DeletionReason)
                .IsRequired(false);

            model
                .Property(tag => tag.PublishDate)
                .IsRequired(false);

            model
                .Property(tag => tag.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(tag => tag.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(ApprovalStatus.Draft);
        }
    }
}
