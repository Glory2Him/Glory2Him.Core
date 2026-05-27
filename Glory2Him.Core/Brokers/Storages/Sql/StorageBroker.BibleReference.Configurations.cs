// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddBibleReferenceConfigurations(EntityTypeBuilder<BibleReference> model)
        {
            model.ToTable("BibleReferences");

            model.HasKey(bibleReference => bibleReference.Id);

            model.Property(bibleReference => bibleReference.Id)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Reference)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Translation)
                 .HasMaxLength(50)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Scripture)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.CreatedWhen)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.UpdatedWhen)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.DeletedWhen)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.DeletionReason)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.ContentItemGroupId)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Version)
                 .IsRequired()
                 .HasDefaultValue(1);

            model.Property(bibleReference => bibleReference.IsLatestVersion)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.PublishDate)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            // Unique version per group
            model.HasIndex(bibleReference => new { bibleReference.ContentItemGroupId, bibleReference.Version })
                 .IsUnique()
                 .HasDatabaseName("UX_BibleReferences_ContentItemGroupId_Version");

            // Exactly one latest version per group
            model.HasIndex(bibleReference => new { bibleReference.ContentItemGroupId, bibleReference.IsLatestVersion })
                 .IsUnique()
                 .HasFilter($"[{nameof(BibleReference.IsLatestVersion)}] = 1")
                 .HasDatabaseName("UX_BibleReferences_ContentItemGroupId_IsLatest");

            // Exactly one published version per group
            model.HasIndex(bibleReference => new { bibleReference.ContentItemGroupId, bibleReference.IsPublished })
                 .IsUnique()
                 .HasFilter($"[{nameof(BibleReference.IsPublished)}] = 1")
                 .HasDatabaseName("UX_BibleReferences_ContentItemGroupId_IsPublished");
        }
    }
}
