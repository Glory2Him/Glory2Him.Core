// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Foundations.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
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

            model.Property(approval => approval.StatusId)
                 .IsRequired();

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

            // UNIQUE (EntityType, EntityId)
            model.HasIndex(a => new { a.EntityType, a.EntityId })
                 .IsUnique()
                 .HasDatabaseName("UX_Approvals_EntityType_EntityId");

            // INDEX (EntityType, StatusId)  -- for common joins/filters
            model.HasIndex(a => new { a.EntityType, a.StatusId })
                 .HasDatabaseName("IX_Approvals_EntityType_StatusId");
        }
    }
}
