// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalSettingConfigurations(EntityTypeBuilder<ApprovalSetting> model)
        {
            model.ToTable("ApprovalSettings");

            model.HasKey(approvalSetting => approvalSetting.Id);

            model.Property(approvalSetting => approvalSetting.Id)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.EntityType)
                 .HasConversion<string>()
                 .HasMaxLength(64)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.RequiredApprovals)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.AllowSelfApproval)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.BlockOnReject)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireReapprovalOnChange)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.AutoApproveIfThresholdMet)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.MustBeInRoleToApprove)
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
                 .IsRequired(false);

            // One approval setting per entity type
            model.HasIndex(approvalSetting => approvalSetting.EntityType)
                 .IsUnique()
                 .HasDatabaseName("UX_ApprovalSettings_EntityType");

            // Relationship: one ApprovalSetting to many ApprovalSettingRoles
            model.HasMany(approvalSetting => approvalSetting.ApprovalSettingRoles)
                 .WithOne(approvalSettingRole => approvalSettingRole.ApprovalSetting)
                 .HasForeignKey(approvalSettingRole => approvalSettingRole.ApprovalSettingId)
                 .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
