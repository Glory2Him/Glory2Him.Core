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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddApprovalReviewConfigurations(EntityTypeBuilder<ApprovalReview> model)
        {
            // Table
            model.ToTable("ApprovalReviews");

            // Key
            model.HasKey(approvalReviews => approvalReviews.Id);
            model.Property(approvalReviews => approvalReviews.ApprovalId).IsRequired();
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
                .HasMaxLength(500)
                .IsRequired(false);

            // Capped to match the foundation's own rule so the column and the service agree
            // rather than the column silently accepting more. Optional, though: a reviewer may
            // approve without justifying it.
            model
                .Property(approvalReviews => approvalReviews.Comment)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Index to speed up joins/filters by parent
            model.HasIndex(approvalReviews => approvalReviews.ApprovalId)
                .HasDatabaseName("IX_ApprovalReviews_ApprovalId");

            // Index to speed up joins/filters by (ApprovalId, StatusId)
            model.HasIndex(approvalReviews => new { approvalReviews.ApprovalId, approvalReviews.StatusId })
                .HasDatabaseName("IX_ApprovalReviews_ApprovalId_StatusId");

            // Ensure each reviewer can only have one ACTIVE review per approval.
            //
            // The filter is the whole point. §7.7 rule 1 bars a second *active* review, not a
            // second review ever — and unfiltered, this index reserved the
            // (ApprovalId, CreatedBy) slot permanently. Withdrawal is a soft delete, so the row
            // stays; dismissal retains the row for audit by design (§9.5). So §7.7 rule 7's
            // "file a new review once yours has been dismissed" had no route at all: rule 1
            // forbids superseding the dismissed row in place, and amending it is refused outright
            // because a dismissal is precisely the record that the verdict no longer describes
            // the current content.
            //
            // The identity half is CreatedBy, not a caller-supplied reviewer id. ReviewerId used
            // to sit here and was free text, which let one reviewer meet a three-approval
            // threshold alone by inventing a second id; binding it to the acting user closed
            // that, and dropping it removes the surface altogether. CreatedBy is written by
            // SecurityAuditBroker from the security context and pinned against storage on modify,
            // so there is no longer a second identity for this index to disagree with.
            model.HasIndex(approvalReviews => new { approvalReviews.ApprovalId, approvalReviews.CreatedBy })
                .IsUnique()
                .HasFilter(
                    $"[{nameof(ApprovalReview.StatusId)}] <> {(int)ApprovalStatus.Dismissed} " +
                        $"AND [{nameof(ApprovalReview.IsDeleted)}] = 0")
                .HasDatabaseName("UX_ApprovalReviews_ApprovalId_CreatedBy");

            // Relationship: many ApprovalReviews to one Approval
            model.HasOne(approvalReview => approvalReview.Approval)
                .WithMany(approval => approval.ApprovalReviews)
                .HasForeignKey(approvalReview => approvalReview.ApprovalId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
