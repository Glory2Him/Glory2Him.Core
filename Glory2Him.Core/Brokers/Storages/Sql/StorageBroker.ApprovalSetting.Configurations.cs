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

            model.Property(approvalSetting => approvalSetting.RequireReapprovalOnChange)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.AutoApproveIfAllApprovalRequirementsMet)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RestrictWhoCanReview)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RestrictWhoCanApprove)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireApprovalCommentResolutionBeforeApproval)
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
                 .IsRequired(false);

            // One approval setting per entity type
            model.HasIndex(approvalSetting => approvalSetting.EntityType)
                 .IsUnique()
                 .HasDatabaseName("UX_ApprovalSettings_EntityType");

            // Relationship: one ApprovalSetting to many ApprovalSettingReviewerRoles
            model.HasMany(approvalSetting => approvalSetting.ApprovalSettingReviewerRoles)
                 .WithOne(approvalSettingReviewerRole => approvalSettingReviewerRole.ApprovalSetting)
                 .HasForeignKey(approvalSettingReviewerRole => approvalSettingReviewerRole.ApprovalSettingId)
                 .OnDelete(DeleteBehavior.NoAction);

            // Relationship: one ApprovalSetting to many ApprovalSettingPublisherRoles
            model.HasMany(approvalSetting => approvalSetting.ApprovalSettingPublisherRoles)
                 .WithOne(approvalSettingPublisherRole => approvalSettingPublisherRole.ApprovalSetting)
                 .HasForeignKey(approvalSettingPublisherRole => approvalSettingPublisherRole.ApprovalSettingId)
                 .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
