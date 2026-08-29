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

using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddApprovalReviewRequestConfigurations(
            EntityTypeBuilder<ApprovalReviewRequest> model)
        {
            // Table
            model.ToTable("ApprovalReviewRequests");

            // Key
            model.HasKey(approvalReviewRequests => approvalReviewRequests.Id);
            model.Property(approvalReviewRequests => approvalReviewRequests.ApprovalId).IsRequired();

            // The invited user's account id. Capped to match CreatedBy on every sibling table,
            // because it is compared against an ApprovalReview's CreatedBy when a request is
            // retired (§7.9 rule 6) — two columns holding the same identity must agree on width.
            model
                .Property(approvalReviewRequests => approvalReviewRequests.RequestedUserId)
                .HasMaxLength(255)
                .IsRequired();

            // Presentation only, and deliberately NOT required: a display name can legitimately
            // be blank in the identity store, and holding the invitation hostage to a cosmetic
            // field would refuse a request the policy allows.
            model
                .Property(approvalReviewRequests => approvalReviewRequests.RequestedUserDisplayName)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(approvalReviewRequests => approvalReviewRequests.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalReviewRequests => approvalReviewRequests.CreatedWhen)
                .IsRequired();

            model
                .Property(approvalReviewRequests => approvalReviewRequests.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approvalReviewRequests => approvalReviewRequests.UpdatedWhen)
                .IsRequired();

            model
                .Property(approvalReviewRequests => approvalReviewRequests.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(approvalReviewRequests => approvalReviewRequests.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(approvalReviewRequests => approvalReviewRequests.DeletedWhen)
                .IsRequired(false);

            model
                .Property(approvalReviewRequests => approvalReviewRequests.DeletionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            // Index to speed up joins/filters by parent — the panel's read is "every pending
            // request for this approval", so this is the access path that matters.
            model.HasIndex(approvalReviewRequests => approvalReviewRequests.ApprovalId)
                .HasDatabaseName("IX_ApprovalReviewRequests_ApprovalId");

            // One ACTIVE invitation per person per approval (§7.9 rule 1).
            //
            // The filter carries the same weight it does on UX_ApprovalReviews_ApprovalId_CreatedBy:
            // withdrawal and answering are both SOFT deletes, so the row stays. Unfiltered, this
            // index would reserve the (ApprovalId, RequestedUserId) slot permanently and a person
            // whose invitation was withdrawn by mistake — the exact case §7.9 rule 5 exists to
            // undo — could never be invited again.
            //
            // No StatusId term, unlike the review index: a request carries no verdict, so the
            // only state that can retire it is deletion.
            model.HasIndex(approvalReviewRequests =>
                    new { approvalReviewRequests.ApprovalId, approvalReviewRequests.RequestedUserId })
                .IsUnique()
                .HasFilter($"[{nameof(ApprovalReviewRequest.IsDeleted)}] = 0")
                .HasDatabaseName("UX_ApprovalReviewRequests_ApprovalId_RequestedUserId");

            // Relationship: many ApprovalReviewRequests to one Approval
            model.HasOne(approvalReviewRequest => approvalReviewRequest.Approval)
                .WithMany(approval => approval.ApprovalReviewRequests)
                .HasForeignKey(approvalReviewRequest => approvalReviewRequest.ApprovalId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
