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
                        name: "CK_Association_ConfidenceScoreRange",
                        sql:
                            $"({nameof(Association.ConfidenceScore)} IS NULL OR " +
                            $"{nameof(Association.ConfidenceScore)} BETWEEN 0 AND 10)");
                });

            model.HasKey(association => association.Id);

            // ── Endpoint A ────────────────────────────────────────────────────────────

            model.Property(association => association.EntityAType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.EntityAKeyId)
                 .IsRequired();

            model.Property(association => association.EntityAGroupId)
                 .IsRequired();

            model.Property(association => association.EntityAScope)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.EntityAContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired(false);

            // ── Endpoint B ────────────────────────────────────────────────────────────

            model.Property(association => association.EntityBType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.EntityBKeyId)
                 .IsRequired();

            model.Property(association => association.EntityBGroupId)
                 .IsRequired();

            model.Property(association => association.EntityBScope)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(association => association.EntityBContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired(false);

            // The effective id is what every read seeks on: "associations for this entity"
            // becomes one comparison instead of an OR across KeyId/GroupId with a scope test
            // per side. Computing it in the database rather than in application code is what
            // lets it be indexed and what makes two rows that mean the same thing collapse
            // to one key — a foundation service is reachable through a public event address
            // and cannot assume an orchestration deduplicated first. The CASE is
            // deterministic, so PERSISTED is legal and the column is seekable.
            model.Property(association => association.EntityAEffectiveId)
                 .HasComputedColumnSql(
                     sql:
                         $"CASE WHEN [{nameof(Association.EntityAScope)}] = " +
                         $"N'{nameof(Scope.AllVersions)}' " +
                         $"THEN [{nameof(Association.EntityAGroupId)}] " +
                         $"ELSE [{nameof(Association.EntityAKeyId)}] END",
                     stored: true);

            model.Property(association => association.EntityBEffectiveId)
                 .HasComputedColumnSql(
                     sql:
                         $"CASE WHEN [{nameof(Association.EntityBScope)}] = " +
                         $"N'{nameof(Scope.AllVersions)}' " +
                         $"THEN [{nameof(Association.EntityBGroupId)}] " +
                         $"ELSE [{nameof(Association.EntityBKeyId)}] END",
                     stored: true);

            // ── Everything else ───────────────────────────────────────────────────────

            model.Property(association => association.UserId)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(association => association.SortOrder)
                 .IsRequired(false);

            // without an explicit precision EF defaults a decimal? to decimal(18,2) on SQL
            // Server — wasteful, and silent about the 0.00-10.00 intent
            model.Property(association => association.ConfidenceScore)
                 .HasPrecision(4, 2)
                 .IsRequired(false);

            model.Property(association => association.ConfidenceReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            model.Property(association => association.SourceBatchId)
                 .IsRequired(false);

            model.Property(association => association.ModelVersion)
                 .HasMaxLength(128)
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

            // the read that runs on every page render — "associations for this entity" —
            // has to check both sides, because canonical ordering means the entity the
            // caller is looking at may be on either
            model.HasIndex(association => new
            {
                association.EntityAType,
                association.EntityAEffectiveId
            })
                 .HasDatabaseName("IX_Associations_EndpointA");

            model.HasIndex(association => new
            {
                association.EntityBType,
                association.EntityBEffectiveId
            })
                 .HasDatabaseName("IX_Associations_EndpointB");
        }
    }
}
