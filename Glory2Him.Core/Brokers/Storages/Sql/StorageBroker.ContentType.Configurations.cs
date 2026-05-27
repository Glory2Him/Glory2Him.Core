// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddContentTypeConfigurations(EntityTypeBuilder<ContentType> model)
        {
            model
                .ToTable("ContentTypes");

            // Primary key
            model
                .HasKey(contentType => contentType.Id);

            model
                .Property(contentType => contentType.Name)
                .IsRequired()
                .HasMaxLength(255);

            model
                .HasIndex(contentType => contentType.Name)
                .IsUnique();

            model
                .Property(contentType => contentType.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentType => contentType.CreatedWhen)
                .IsRequired();

            model
                .Property(contentType => contentType.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentType => contentType.UpdatedWhen)
                .IsRequired();

            model
                .Property(contentType => contentType.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentType => contentType.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(contentType => contentType.DeletedWhen)
                .IsRequired(false);

            model
                .Property(contentType => contentType.DeletionReason)
                .IsRequired(false);

            model
                .Property(contentType => contentType.PublishDate)
                .IsRequired(false);

            model
                .Property(contentType => contentType.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentType => contentType.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(ApprovalStatus.Draft);
        }
    }
}
