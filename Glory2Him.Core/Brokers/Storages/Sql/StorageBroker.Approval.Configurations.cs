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

using Glory2Him.Core.Models.Foundations.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddApprovalConfigurations(EntityTypeBuilder<Approval> model)
        {
            // Table
            model.ToTable("Approvals");

            // Key
            model.HasKey(approval => approval.Id);

            model.Property(approval => approval.EntityType)
                 .IsRequired();

            model.Property(approval => approval.EntityId)
                 .IsRequired();

            model.Property(approval => approval.ApprovalStatus)
                 .IsRequired();

            model.Property(approval => approval.IsApprovedByBypass)
                 .IsRequired()
                 .HasDefaultValue(false);

            model
                .Property(approval => approval.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approval => approval.CreatedWhen)
                .IsRequired();

            model
                .Property(approval => approval.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired();

            model
                .Property(approval => approval.UpdatedWhen)
                .IsRequired();

            model
                .Property(approval => approval.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            model
                .Property(approval => approval.DeletedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            model
                .Property(approval => approval.DeletedWhen)
                .IsRequired(false);

            model
                .Property(approval => approval.DeletionReason)
                .IsRequired(false);

            // UNIQUE (EntityType, EntityId)
            model.HasIndex(a => new { a.EntityType, a.EntityId })
                 .IsUnique()
                 .HasDatabaseName("UX_Approvals_EntityType_EntityId");

            // INDEX (EntityType, StatusId)  -- for common joins/filters
            model.HasIndex(a => new { a.EntityType, a.ApprovalStatus })
                 .HasDatabaseName("IX_Approvals_EntityType_StatusId");
        }
    }
}
