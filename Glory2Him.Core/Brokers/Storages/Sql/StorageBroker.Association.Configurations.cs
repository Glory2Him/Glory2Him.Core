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
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddAssociationConfigurations(EntityTypeBuilder<Association> model)
        {
            model.ToTable(
                "Associations",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        name: "CK_Association_ScopeConsistency",
                        sql:
                            $"(({nameof(Association.LinkedContentScope)} = " +
                            $"N'{nameof(Scope.AllVersions)}' AND " +
                            $"{nameof(Association.LinkedContentItemGroupId)} IS NOT NULL AND " +
                            $"{nameof(Association.LinkedContentItemId)} IS NULL) OR " +
                            $"({nameof(Association.LinkedContentScope)} = " +
                            $"N'{nameof(Scope.ThisVersionOnly)}' AND " +
                            $"{nameof(Association.LinkedContentItemId)} IS NOT NULL AND " +
                            $"{nameof(Association.LinkedContentItemGroupId)} IS NULL))");

                    tableBuilder.HasCheckConstraint(
                        name: "CK_Association_AssociationConfidenceScoreRange",
                        sql:
                            $"({nameof(Association.AssociationConfidenceScore)} IS NULL OR " +
                            $"{nameof(Association.AssociationConfidenceScore)} BETWEEN 0 AND 10)");
                });

            model.HasKey(association => association.Id);

            model.Property(association => association.LinkedContentScope)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.LinkedEntityType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.LinkedContentItemId)
                 .IsRequired(false);

            model.Property(association => association.LinkedContentItemGroupId)
                 .IsRequired(false);

            model.Property(association => association.LinkedEntityId)
                 .IsRequired();

            model.Property(association => association.AssociationConfidenceScore)
                 .IsRequired(false);

            model.Property(association => association.AssociationConfidenceReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            model.Property(association => association.CreatedBy)
                 .IsRequired()
                 .HasMaxLength(256);

            model.Property(association => association.UpdatedBy)
                 .IsRequired()
                 .HasMaxLength(256);

            model.Property(association => association.CreatedWhen)
                 .IsRequired();

            model.Property(association => association.UpdatedWhen)
                 .IsRequired();

            model.Property(association => association.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(association => association.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(association => association.DeletedWhen)
                 .IsRequired(false);

            model.Property(association => association.DeletionReason)
                 .IsRequired(false);

            model.Property(association => association.PublishDate)
                 .IsRequired(false);

            model.Property(association => association.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(association => association.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            // By target entity (what this association points to)
            model.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId })
                 .HasDatabaseName("IX_Association_Target");

            // By scope: all versions of an associated content item group
            model.HasIndex(e => new { e.LinkedContentScope, e.LinkedContentItemGroupId })
                 .HasFilter(
                     $"[{nameof(Association.LinkedContentScope)}] = N'{nameof(Scope.AllVersions)}'")
                 .HasDatabaseName("IX_Association_ByAssociatedContentItemGroupId_ScopeAll");

            // By scope: a specific version of an associated content item
            model.HasIndex(e => new { e.LinkedContentScope, e.LinkedContentItemId })
                 .HasFilter(
                     $"[{nameof(Association.LinkedContentScope)}] = N'{nameof(Scope.ThisVersionOnly)}'")
                 .HasDatabaseName("IX_Association_ByItem_ScopeThis");
        }
    }
}
