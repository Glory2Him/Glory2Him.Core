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
using Glory2Him.Core.Models.Foundations.Approvals;
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

            // The round has to EXIST. That is a different question from the index above, which
            // says at most one LIVE assignment per round — neither substitutes for the other,
            // and without this one nothing between a fabricated ApprovalId on the
            // AIReviewerAssignment-Adding address and the table refuses it:
            // AIReviewerAssignmentService holds no IAccessBroker and checks ApprovalId with
            // IsInvalid only.
            //
            // NAVIGATIONLESS ON BOTH SIDES, and that is the point rather than a shortcut. The
            // generic HasOne<Approval>() with an argumentless WithMany() declares the constraint
            // and leaves Approval byte for byte as it was — no reverse collection, which is the
            // cost this relationship was once thought to carry. The three sibling carriers
            // (ApprovalComment, ApprovalReview, ApprovalReviewRequest) keep their navigations;
            // they are not harmonised to this shape, and this one is not harmonised to theirs.
            //
            // NoAction, matching those three: destroying a round never destroys what hangs off
            // it. The key says nothing about whether the round is LIVE — an assignment against a
            // soft-deleted round is still accepted, because liveness stays with IAccessBroker
            // (§APR8.6.1).
            model.HasOne<Approval>()
                .WithMany()
                .HasForeignKey(aiReviewerAssignment => aiReviewerAssignment.ApprovalId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
        }
    }
}
