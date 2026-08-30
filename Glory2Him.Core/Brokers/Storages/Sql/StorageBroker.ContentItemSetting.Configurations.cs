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

using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddContentItemSettingConfigurations(EntityTypeBuilder<ContentItemSetting> model)
        {
            model
                .ToTable("ContentItemSettings");

            // Primary key
            model.HasKey(contentItemSetting => contentItemSetting.Id);

            model.Property(contentItemSetting => contentItemSetting.ContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(contentItemSetting => contentItemSetting.ContentItemId)
                 .IsRequired(false);

            model.Property(contentItemSetting => contentItemSetting.HasTitle)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.HasAuthor)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.IsAvailableAsGeneralUserContribution)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ContentTypeName)
                .HasMaxLength(50)
                .IsRequired(false);

            model.Property(contentItemSetting => contentItemSetting.ContentTypeDescription)
                .HasMaxLength(500)
                .IsRequired(false);

            model.Property(contentItemSetting => contentItemSetting.ContentTypeIconCssClass)
                .IsRequired(false);

            model.Property(contentItemSetting => contentItemSetting.TagsAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowTags)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.ReactionsAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowReactions)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.LinksAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowLinks)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.AttachmentsAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowAttachments)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.CommentsAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowComments)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.BibleReferenceAllowed)
                .IsRequired()
                .HasDefaultValue(false);

            model.Property(contentItemSetting => contentItemSetting.ShowBibleReferences)
                .IsRequired()
                .HasDefaultValue(true);

            model.Property(contentItemSetting => contentItemSetting.LimitReactionsToLoveOnly)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentItemSetting => contentItemSetting.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentItemSetting => contentItemSetting.CreatedWhen)
                .IsRequired();

            model
                .Property(contentItemSetting => contentItemSetting.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(contentItemSetting => contentItemSetting.UpdatedWhen)
                .IsRequired();

            model
                .Property(contentItemSetting => contentItemSetting.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(contentItemSetting => contentItemSetting.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(contentItemSetting => contentItemSetting.DeletedWhen)
                .IsRequired(false);

            model
                .Property(contentItemSetting => contentItemSetting.DeletionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            // ------------------------------------------------------------------------
            // Filtered unique indexes (SQL Server) to enforce your business rules:
            // 1) At most one LIVE default per type:
            //      UNIQUE(ContentType) WHERE ContentItemId IS NULL AND IsDeleted = 0
            // 2) At most one LIVE override per entity/post:
            //      UNIQUE(ContentItemId) WHERE ContentItemId IS NOT NULL AND IsDeleted = 0
            //
            // The IsDeleted term is load-bearing rather than tidy. It is what §12.5.2 business
            // rules 3-4 mean by one default per content type and one override per item: a
            // soft-deleted row is invisible to every caller including Admin (§14.5 rule 3), so
            // one occupying a scope could be neither seen nor moved — and the API's delete is a
            // SOFT delete, which made the ordinary way to remove a setting the way that trapped
            // its content type, or its content item, forever.
            // ------------------------------------------------------------------------

            model.HasIndex(contentItemSetting => contentItemSetting.ContentType)
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ContentItemSetting.ContentItemId)}] IS NULL AND " +
                     $"[{nameof(ContentItemSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ContentItemSettings_DefaultPerType");

            model.HasIndex(contentItemSetting => contentItemSetting.ContentItemId)
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ContentItemSetting.ContentItemId)}] IS NOT NULL AND " +
                     $"[{nameof(ContentItemSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ContentItemSettings_OverridePerEntity");
        }
    }
}
