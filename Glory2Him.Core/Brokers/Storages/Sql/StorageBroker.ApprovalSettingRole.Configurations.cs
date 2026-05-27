// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalSettingRoleConfigurations(EntityTypeBuilder<ApprovalSettingRole> model)
        {
            model.ToTable("ApprovalSettingRoles");

            model.HasKey(approvalSettingRole => approvalSettingRole.Id);

            model.Property(approvalSettingRole => approvalSettingRole.Id)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.ApprovalSettingId)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.RoleName)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.CreatedWhen)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.UpdatedWhen)
                 .IsRequired();

            model.Property(approvalSettingRole => approvalSettingRole.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSettingRole => approvalSettingRole.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(approvalSettingRole => approvalSettingRole.DeletedWhen)
                 .IsRequired(false);

            model.Property(approvalSettingRole => approvalSettingRole.DeletionReason)
                 .IsRequired(false);

            // Index on parent setting for join/filter performance
            model.HasIndex(approvalSettingRole => approvalSettingRole.ApprovalSettingId)
                 .HasDatabaseName("IX_ApprovalSettingRoles_ApprovalSettingId");

            // Unique: one role name per approval setting
            model.HasIndex(approvalSettingRole => new
                {
                    approvalSettingRole.ApprovalSettingId,
                    approvalSettingRole.RoleName
                })
                 .IsUnique()
                 .HasDatabaseName("UX_ApprovalSettingRoles_ApprovalSettingId_RoleName");
        }
    }
}
