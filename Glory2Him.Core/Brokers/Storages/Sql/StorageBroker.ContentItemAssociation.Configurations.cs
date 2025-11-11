// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
    {
        private static void AddContentItemAssociationConfigurations(EntityTypeBuilder<ContentItemAssociation> model)
        {
            model.ToTable(
                "ContentItemAssociations",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ContentItemAssociation_ScopeConsistency",
                        sql:
                            $"(({nameof(ContentItemAssociation.Scope)} = N'{nameof(Scope.AllVersions)}' AND " +
                            $"{nameof(ContentItemAssociation.ContentItemGroupId)} IS NOT NULL AND " +
                            $"{nameof(ContentItemAssociation.ContentItemId)} IS NULL) OR " +
                            $"({nameof(ContentItemAssociation.Scope)} = N'{nameof(Scope.ThisVersionOnly)}' AND " +
                            $"{nameof(ContentItemAssociation.ContentItemId)} IS NOT NULL AND " +
                            $"{nameof(ContentItemAssociation.ContentItemGroupId)} IS NULL))");
                });

            model.HasKey(e => e.Id);

            model.Property(e => e.Scope)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(e => e.EntityType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(e => e.ContentItemId).IsRequired(false);
            model.Property(e => e.ContentItemGroupId).IsRequired(false);
            model.Property(e => e.EntityId).IsRequired();
            model.Property(e => e.ApprovalId).IsRequired();
            model.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
            model.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(256);
            model.Property(e => e.CreatedWhen).IsRequired();
            model.Property(e => e.UpdatedWhen).IsRequired();

            model.HasIndex(e => new { e.EntityType, e.EntityId })
                 .HasDatabaseName("IX_ContentItemAssociation_Target");

            model.HasIndex(e => new { e.Scope, e.ContentItemGroupId })
                 .HasFilter($"[{nameof(ContentItemAssociation.Scope)}] = N'{nameof(Scope.AllVersions)}'")
                 .HasDatabaseName("IX_ContentItemAssociation_ByCorrelation_ScopeAll");

            model.HasIndex(e => new { e.Scope, e.ContentItemId })
                 .HasFilter($"[{nameof(ContentItemAssociation.Scope)}] = N'{nameof(Scope.ThisVersionOnly)}'")
                 .HasDatabaseName("IX_ContentItemAssociation_ByItem_ScopeThis");

            // Primary key
            model
                .HasKey(contentItemAssociation => contentItemAssociation.Id);

            // Properties
            model
                .Property(contentItemAssociation => contentItemAssociation.Scope)
                .IsRequired();

            model
                .Property(contentItemAssociation => contentItemAssociation.ContentItemId)
                .IsRequired(false);

            model
                .Property(contentItemAssociation => contentItemAssociation.ContentItemGroupId)
                .IsRequired(false);

            model
                .Property(contentItemAssociation => contentItemAssociation.EntityType)
                .IsRequired();

            model
                .Property(contentItemAssociation => contentItemAssociation.EntityId)
                .IsRequired();

            model
                .Property(contentItemAssociation => contentItemAssociation.ApprovalId)
                .IsRequired();

            model
                .Property(contentItemAssociation => contentItemAssociation.CreatedBy)
                .IsRequired()
                .HasMaxLength(256);

            model
                .Property(contentItemAssociation => contentItemAssociation.UpdatedBy)
                .IsRequired()
                .HasMaxLength(256);

            model
                .Property(contentItemAssociation => contentItemAssociation.CreatedWhen)
                .IsRequired();

            model
                .Property(contentItemAssociation => contentItemAssociation.UpdatedWhen)
                .IsRequired();

            // Indexes to support common queries

            // By target entity (what this association points to)
            model.HasIndex(e => new
            {
                e.EntityType,
                e.EntityId
            })
            .HasDatabaseName("IX_ContentItemAssociation_Target");

            // Look up associations that apply to ALL VERSIONS of a group
            model.HasIndex(e => new { e.Scope, e.ContentItemGroupId })
                 .HasFilter($"[{nameof(ContentItemAssociation.Scope)}] = 0")
                 .HasDatabaseName("IX_ContentItemAssociation_ByCorrelation_Scope0");

            // Look up associations that apply to THIS VERSION ONLY
            model.HasIndex(e => new { e.Scope, e.ContentItemId })
                 .HasFilter($"[{nameof(ContentItemAssociation.Scope)}] = 1")
                 .HasDatabaseName("IX_ContentItemAssociation_ByItem_Scope1");
        }
    }
}
