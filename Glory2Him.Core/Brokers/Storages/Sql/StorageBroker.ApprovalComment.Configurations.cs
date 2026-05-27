// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalCommentConfigurations(EntityTypeBuilder<ApprovalComment> model)
        {
            // Table
            model.ToTable("ApprovalComments");

            // Key
            model.HasKey(approvalComment => approvalComment.Id);

            model
                .Property(approvalComment => approvalComment.ApprovalId)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.UserId)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.CreatedWhen)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.UpdatedWhen)
                .IsRequired();

            model
                .Property(approvalComment => approvalComment.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(approvalComment => approvalComment.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(approvalComment => approvalComment.DeletedWhen)
                .IsRequired(false);

            model
                .Property(approvalComment => approvalComment.DeletionReason)
                .IsRequired(false);

            model.Property(approvalComment => approvalComment.Comment).IsRequired(false);

            // Index to speed up joins/filters by parent
            model.HasIndex(approvalComment => approvalComment.ApprovalId)
                 .HasDatabaseName("IX_ApprovalComments_ApprovalId");

            // Relationship: many ApprovalComments to one Approval
            model.HasOne(approvalComment => approvalComment.Approval)
                .WithMany(approval => approval.ApprovalComments)
                .HasForeignKey(approvalComment => approvalComment.ApprovalId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
