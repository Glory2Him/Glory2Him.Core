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

using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalSettingPublisherRoleConfigurations(EntityTypeBuilder<ApprovalSettingPublisherRole> model)
        {
            model.ToTable("ApprovalSettingPublisherRoles");

            model.HasKey(approvalSettingPublisherRole => approvalSettingPublisherRole.Id);

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.Id)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.ApprovalSettingId)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.RoleName)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.CreatedWhen)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.UpdatedWhen)
                 .IsRequired();

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.DeletedWhen)
                 .IsRequired(false);

            model.Property(approvalSettingPublisherRole => approvalSettingPublisherRole.DeletionReason)
                 .IsRequired(false);

            // Index on parent setting for join/filter performance
            model.HasIndex(approvalSettingPublisherRole => approvalSettingPublisherRole.ApprovalSettingId)
                 .HasDatabaseName("IX_ApprovalSettingPublisherRoles_ApprovalSettingId");

            // Unique: one role name per approval setting
            model.HasIndex(approvalSettingPublisherRole => new
            {
                approvalSettingPublisherRole.ApprovalSettingId,
                approvalSettingPublisherRole.RoleName
            })
                 .IsUnique()
                 .HasDatabaseName("UX_ApprovalSettingPublisherRoles_ApprovalSettingId_RoleName");
        }
    }
}
