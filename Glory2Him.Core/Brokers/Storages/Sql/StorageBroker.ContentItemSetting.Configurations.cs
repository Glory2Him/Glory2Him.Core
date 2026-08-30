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

            // The default matches the entity's own, so a row inserted without one lands past
            // the curated seed values rather than at zero, where every unordered row would
            // otherwise pile up ahead of the types somebody chose the order of.
            model.Property(contentItemSetting => contentItemSetting.SortOrder)
                .IsRequired()
                .HasDefaultValue(1000);

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
            // 1) At most one default per type:
            //      UNIQUE(ContentType) WHERE ContentItemId IS NULL
            // 2) At most one override per entity/post:
            //      UNIQUE(ContentItemId) WHERE ContentItemId IS NOT NULL
            // ------------------------------------------------------------------------

            model.HasIndex(contentItemSetting => contentItemSetting.ContentType)
                 .IsUnique()
                 .HasFilter($"[{nameof(ContentItemSetting.ContentItemId)}] IS NULL")
                 .HasDatabaseName("UX_ContentItemSettings_DefaultPerType");

            model.HasIndex(contentItemSetting => contentItemSetting.ContentItemId)
                 .IsUnique()
                 .HasFilter($"[{nameof(ContentItemSetting.ContentItemId)}] IS NOT NULL")
                 .HasDatabaseName("UX_ContentItemSettings_OverridePerEntity");
        }
    }
}
