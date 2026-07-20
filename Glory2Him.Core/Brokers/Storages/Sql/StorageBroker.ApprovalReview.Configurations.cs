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

            model
                .Property(approvalReviews => approvalReviews.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(approvalReviews => approvalReviews.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(approvalReviews => approvalReviews.DeletedWhen)
                .IsRequired(false);

            model
                .Property(approvalReviews => approvalReviews.DeletionReason)
                .IsRequired(false);

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
