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
using Glory2Him.Core.Models.Foundations.Attachments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddAttachmentConfigurations(EntityTypeBuilder<Attachment> model)
        {
            model.ToTable("Attachments");

            model.HasKey(attachment => attachment.Id);

            model.Property(attachment => attachment.Id)
                 .IsRequired();

            model.Property(attachment => attachment.Name)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(attachment => attachment.BlobUri)
                 .HasMaxLength(2048)
                 .IsRequired();

            model.Property(attachment => attachment.Hash)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(attachment => attachment.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(attachment => attachment.CreatedWhen)
                 .IsRequired();

            model.Property(attachment => attachment.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(attachment => attachment.UpdatedWhen)
                 .IsRequired();

            model.Property(attachment => attachment.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(attachment => attachment.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(attachment => attachment.DeletedWhen)
                 .IsRequired(false);

            model.Property(attachment => attachment.DeletionReason)
                 .IsRequired(false);

            model.Property(attachment => attachment.ContentItemGroupId)
                 .IsRequired();

            model.Property(attachment => attachment.Version)
                 .IsRequired()
                 .HasDefaultValue(1);

            model.Property(attachment => attachment.IsLatestVersion)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(attachment => attachment.PublishDate)
                 .IsRequired(false);

            model.Property(attachment => attachment.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(attachment => attachment.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            model.Property(attachment => attachment.IsApprovedByBypass)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(attachment => attachment.ApprovedByBypassReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            // Unique version per group
            model.HasIndex(attachment => new { attachment.ContentItemGroupId, attachment.Version })
                 .IsUnique()
                 .HasDatabaseName("UX_Attachments_ContentItemGroupId_Version");

            // Exactly one latest version per group
            model.HasIndex(attachment => new { attachment.ContentItemGroupId, attachment.IsLatestVersion })
                 .IsUnique()
                 .HasFilter($"[{nameof(Attachment.IsLatestVersion)}] = 1")
                 .HasDatabaseName("UX_Attachments_ContentItemGroupId_G2Hatest");

            // Exactly one published version per group
            model.HasIndex(attachment => new { attachment.ContentItemGroupId, attachment.IsPublished })
                 .IsUnique()
                 .HasFilter($"[{nameof(Attachment.IsPublished)}] = 1")
                 .HasDatabaseName("UX_Attachments_ContentItemGroupId_IsPublished");

            // Hash index for deduplication lookups
            model.HasIndex(attachment => attachment.Hash)
                 .HasDatabaseName("IX_Attachments_Hash");
        }
    }
}
