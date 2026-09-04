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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
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
                .Property(contentItem => contentItem.ContentType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsUnicode(true)
                .IsRequired();

            model.Property(contentItem => contentItem.Content)
                .IsRequired();

            model
                .Property(contentItem => contentItem.ShareabilityBasis)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsUnicode(true)
                .IsRequired()
                .HasDefaultValue(ShareabilityBasis.Owned);

            model
                .Property(contentItem => contentItem.SharePermission)
                .HasMaxLength(500)
                .IsRequired(false);

            model.Property(contentItem => contentItem.ContentHash)
                .HasMaxLength(64)
                .IsRequired();

            model.Property(contentItem => contentItem.GroupId)
                .IsRequired();

            // ValueGeneratedNever for the reason ContentItemSetting.SortOrder carries it
            // (#395): a store default alone makes EF omit a CLR-default 0 from the insert, and
            // the column writes 1 in its place. Version 0 is never legitimate here - versions
            // start at 1 - so nothing has been lost, but that is a decision rather than an
            // accident of which number the CLR happens to default to.
            model.Property(contentItem => contentItem.Version)
                .HasDefaultValue(1)
                .IsRequired()
                .ValueGeneratedNever();

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

            model
                .Property(contentItem => contentItem.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentItem => contentItem.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(contentItem => contentItem.DeletedWhen)
                .IsRequired(false);

            model
                .Property(contentItem => contentItem.DeletionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            model
                .Property(contentItem => contentItem.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentItem => contentItem.PublishDate)
                .IsRequired(false);

            model
                .Property(contentItem => contentItem.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(ApprovalStatus.Draft);

            model
                .Property(contentItem => contentItem.IsApprovedByBypass)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentItem => contentItem.ApprovedByBypassReason)
                .HasMaxLength(500)
                .IsRequired(false);

            // Optional properties
            model.Property(contentItem => contentItem.Title);
            model.Property(contentItem => contentItem.Author);

            // Index on (GroupId, Version DESC) for "latest" lookups
            // SQL Server supports DESC index sort order explicitly
            model.HasIndex(contentItem => new { contentItem.GroupId, contentItem.Version })
                .HasDatabaseName("IX_ContentItems_GroupId_VersionDesc")
                .IsUnique()
                .IsDescending(true, true);

            AddPublishedSlotIndex(model, "IX_ContentItem_IsPublished");

            // §14.2 — additional recommended indexes
            model.HasIndex(contentItem => contentItem.ContentType)
                 .HasDatabaseName("IX_ContentItems_ContentType");

            model.HasIndex(contentItem => contentItem.PublishDate)
                 .HasDatabaseName("IX_ContentItems_PublishDate");

            model.HasIndex(contentItem => new
            {
                contentItem.ApprovalStatus,
                contentItem.IsPublished,
                contentItem.PublishDate
            })
                 .HasDatabaseName("IX_ContentItems_Feed");

            model.HasIndex(contentItem => contentItem.DeletedWhen)
                 .HasDatabaseName("IX_ContentItems_DeletedWhen");

            // §3.4.2 — duplicate content detection. Deliberately NOT unique: rows within one
            // group may legitimately share a hash (e.g. a later version reverting to earlier
            // wording); uniqueness is enforced application-side by the orchestration.
            model.HasIndex(contentItem => new
            {
                contentItem.ContentType,
                contentItem.ContentHash
            })
                 .HasDatabaseName("IX_ContentItems_ContentTypeId_ContentHash");
        }
    }
}
