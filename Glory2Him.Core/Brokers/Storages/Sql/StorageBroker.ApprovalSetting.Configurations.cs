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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddApprovalSettingConfigurations(EntityTypeBuilder<ApprovalSetting> model)
        {
            model.ToTable(
                "ApprovalSettings",
                tableBuilder =>
                {
                    // design §8.4: ContentType may be populated only when EntityType = ContentItem
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                        sql:
                            $"({nameof(ApprovalSetting.ContentType)} IS NULL OR " +
                            $"{nameof(ApprovalSetting.EntityType)} = N'{nameof(EntityType.ContentItem)}')");
                });

            model.HasKey(approvalSetting => approvalSetting.Id);

            model.Property(approvalSetting => approvalSetting.Id)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.EntityType)
                 .HasConversion<string>()
                 .HasMaxLength(64)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.ContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.RequireApprovals)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.RequiredNumberOfApprovals)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.AllowSelfApproval)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.BlockOnReject)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.BlockOnZeroApprovalScore)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireReapprovalOnChange)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.AutoApproveIfAllApprovalRequirementsMet)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireReviewCommentResolutionBeforeApprovals)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.DoNotAllowBypassingSettings)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.CreatedWhen)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.UpdatedWhen)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.DeletedWhen)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.DeletionReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            // design §8.4 resolution tiers — a single UNIQUE(EntityType) would prevent any
            // entity type from ever having more than one row, incompatible with the
            // per-content-type tier. NULL/NULL is not distinct in SQL Server's default
            // unique-index semantics, so this needs two filtered indexes rather than one:

            // 1) at most one entity-type-level default (ContentType IS NULL)
            model.HasIndex(approvalSetting => approvalSetting.EntityType)
                 .IsUnique()
                 .HasFilter($"[{nameof(ApprovalSetting.ContentType)}] IS NULL")
                 .HasDatabaseName("UX_ApprovalSettings_EntityTypeDefault");

            // 2) at most one row per (EntityType, ContentType) when ContentType is populated
            model.HasIndex(approvalSetting => new { approvalSetting.EntityType, approvalSetting.ContentType })
                 .IsUnique()
                 .HasFilter($"[{nameof(ApprovalSetting.ContentType)}] IS NOT NULL")
                 .HasDatabaseName("UX_ApprovalSettings_EntityTypeContentType");
        }
    }
}
