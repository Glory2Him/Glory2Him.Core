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

using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddAIReviewerAssignmentConfigurations(
            EntityTypeBuilder<AIReviewerAssignment> model)
        {
            // Table
            model.ToTable("AIReviewerAssignments");

            // Key
            model.HasKey(aiReviewerAssignment => aiReviewerAssignment.Id);

            // A plain column, deliberately with no HasOne/WithMany/HasForeignKey — see
            // AIReviewerAssignment.ApprovalId for why. The filtered unique index below is what
            // actually enforces the one-live-assignment invariant.
            model.Property(aiReviewerAssignment => aiReviewerAssignment.ApprovalId).IsRequired();

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.IsAIReviewCompleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.IsAIReviewCommentsPresent)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.CreatedWhen)
                .IsRequired();

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.UpdatedWhen)
                .IsRequired();

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.DeletedWhen)
                .IsRequired(false);

            model
                .Property(aiReviewerAssignment => aiReviewerAssignment.DeletionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            // One LIVE assignment per approval — the whole reason this entity is simpler than
            // ApprovalReviewRequest's per-person index: there is no second dimension to key on,
            // so the filter alone (not a composite key) is what makes this mean anything.
            //
            // Filtered rather than plain-unique for the same reason every soft-delete-aware index
            // in this store is: a removed assignment must free the slot so Berean can be assigned
            // to the round again, and an unfiltered index would reserve ApprovalId permanently
            // after the first (and only) assignment ever made.
            model.HasIndex(aiReviewerAssignment => aiReviewerAssignment.ApprovalId)
                .IsUnique()
                .HasFilter($"[{nameof(AIReviewerAssignment.IsDeleted)}] = 0")
                .HasDatabaseName("UX_AIReviewerAssignments_ApprovalId");
        }
    }
}
