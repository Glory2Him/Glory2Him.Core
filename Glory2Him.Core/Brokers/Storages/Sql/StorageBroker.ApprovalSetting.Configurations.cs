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

                    // design §8.4: IsPersonal may be populated only when EntityType = Association.
                    // The same shape as the constraint above, for the same reason — the
                    // personality of a row is a property of an association's UserId (§4.2) and
                    // means nothing on any other entity type.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                        sql:
                            $"({nameof(ApprovalSetting.IsPersonal)} IS NULL OR " +
                            $"{nameof(ApprovalSetting.EntityType)} = N'{nameof(EntityType.Association)}')");
                });

            model.HasKey(approvalSetting => approvalSetting.Id);

            model.Property(approvalSetting => approvalSetting.Id)
                 .IsRequired();

            // NULLABLE, and that is the global tier (design §8.4): a row with no entity type
            // is the one every entity-type default narrows.
            model.Property(approvalSetting => approvalSetting.EntityType)
                 .HasConversion<string>()
                 .HasMaxLength(64)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.ContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.IsPersonal)
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

            // Both carry an IsDeleted term, and it is load-bearing rather than tidy. §8.4
            // resolution skips soft-deleted rows at every tier and §14.5 rule 3 hides them from
            // every caller including Administrators, so a deleted row occupying a scope would be
            // invisible and immovable — and the API's delete is a SOFT delete, which made the
            // ordinary way to remove a policy the way that trapped its scope forever. There are
            // eight EntityType members with one default slot each; a trapped one could never be
            // re-created.

            // 1) at most one LIVE global default (EntityType IS NULL). The key column is all
            //    NULL on every row this filter admits, and NULL equals NULL under unique-index
            //    semantics — which is exactly what makes it a single slot rather than none.
            //
            //    NAMED IN THE CALL, as is the next one: EF keys an index on its property list,
            //    so two HasIndex calls over the same column silently replace each other unless
            //    the name is part of the definition. The first cut of this lost the global
            //    index that way and the migration never created it.
            model.HasIndex(
                     approvalSetting => approvalSetting.EntityType,
                     "UX_ApprovalSettings_GlobalDefault")
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.EntityType)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0");

            // 2) at most one LIVE entity-type-level default (no narrowing on either axis)
            model.HasIndex(
                     approvalSetting => approvalSetting.EntityType,
                     "UX_ApprovalSettings_EntityTypeDefault")
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.EntityType)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.ContentType)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsPersonal)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0");

            // 3) at most one LIVE row per (EntityType, ContentType) when ContentType is populated
            model.HasIndex(approvalSetting => new { approvalSetting.EntityType, approvalSetting.ContentType })
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.ContentType)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ApprovalSettings_EntityTypeContentType");

            // 4) at most one LIVE row per (EntityType, IsPersonal) when IsPersonal is populated —
            //    one personal and one editorial policy for associations, at most.
            model.HasIndex(approvalSetting => new { approvalSetting.EntityType, approvalSetting.IsPersonal })
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.IsPersonal)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ApprovalSettings_AssociationPersonality");
        }
    }
}
