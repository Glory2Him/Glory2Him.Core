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

using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalSettingReviewerRoleConfigurations(EntityTypeBuilder<ApprovalSettingReviewerRole> model)
        {
            model.ToTable("ApprovalSettingReviewerRoles");

            model.HasKey(approvalSettingReviewerRole => approvalSettingReviewerRole.Id);

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.Id)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.ApprovalSettingId)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.RoleName)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.CreatedWhen)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.UpdatedWhen)
                 .IsRequired();

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.DeletedWhen)
                 .IsRequired(false);

            model.Property(approvalSettingReviewerRole => approvalSettingReviewerRole.DeletionReason)
                 .IsRequired(false);

            // Index on parent setting for join/filter performance
            model.HasIndex(approvalSettingReviewerRole => approvalSettingReviewerRole.ApprovalSettingId)
                 .HasDatabaseName("IX_ApprovalSettingReviewerRoles_ApprovalSettingId");

            // Unique: one role name per approval setting
            model.HasIndex(approvalSettingReviewerRole => new
            {
                approvalSettingReviewerRole.ApprovalSettingId,
                approvalSettingReviewerRole.RoleName
            })
                 .IsUnique()
                 .HasDatabaseName("UX_ApprovalSettingReviewerRoles_ApprovalSettingId_RoleName");
        }
    }
}
