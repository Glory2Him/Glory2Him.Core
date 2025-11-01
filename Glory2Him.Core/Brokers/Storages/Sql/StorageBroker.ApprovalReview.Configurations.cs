// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddApprovalReviewConfigurations(EntityTypeBuilder<ApprovalReview> model)
        {
            // Table
            model.ToTable("ApprovalReviews");

            // Key
            model.HasKey(approvalReviews => approvalReviews.Id);
            model.Property(approvalReviews => approvalReviews.ApprovalId).IsRequired();
            model.Property(approvalReviews => approvalReviews.ReviewerId).IsRequired();
            model.Property(approvalReviews => approvalReviews.StatusId).IsRequired();

            model
                .Property(approvalReviews => approvalReviews.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalReviews => approvalReviews.CreatedWhen)
                .IsRequired();

            model
                .Property(approvalReviews => approvalReviews.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalReviews => approvalReviews.UpdatedWhen)
                .IsRequired();

            model.Property(approvalReviews => approvalReviews.Comment).IsRequired(false);

            // Index to speed up joins/filters by parent
            model.HasIndex(approvalReviews => approvalReviews.ApprovalId)
                .HasDatabaseName("IX_ApprovalReviews_ApprovalId");

            // Index to speed up joins/filters by (ApprovalId, StatusId)
            model.HasIndex(approvalReviews => new { approvalReviews.ApprovalId, approvalReviews.StatusId })
                .HasDatabaseName("IX_ApprovalReviews_ApprovalId_StatusId");

            // Ensure each reviewer can only have one review per approval
            model.HasIndex(approvalReviews => new { approvalReviews.ApprovalId, approvalReviews.ReviewerId })
                .IsUnique()
                .HasDatabaseName("UX_ApprovalReviews_ApprovalId_ReviewerId");

            // Relationship: many ApprovalReviews to one Approval
            model.HasOne(approvalReview => approvalReview.Approval)
                .WithMany(approval => approval.ApprovalReviews)
                .HasForeignKey(approvalReview => approvalReview.ApprovalId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
