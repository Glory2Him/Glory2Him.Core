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

                    // An endpoint cannot be associated with itself. The service already
                    // refuses it, but the group ids are what canonical ordering compares, so
                    // an equal pair would also make the ordering constraint below
                    // unsatisfiable — stating it separately gives the clearer violation.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_Association_NotSameGroup",
                        sql:
                            $"[{nameof(Association.EntityAGroupId)}] <> " +
                            $"[{nameof(Association.EntityBGroupId)}]");

                    // The database half of canonical ordering (§4). Without it the uniqueness
                    // index below is worth nothing: the same pair written the other way round
                    // is a different key, and the duplicate lands.
                    //
                    // COLLATE Latin1_General_BIN2 is applied to the EXPRESSION, not the
                    // column, so storage collation and every other comparison are untouched.
                    // It is required, not decorative: the database default here is
                    // SQL_Latin1_General_CP1_CI_AS, which is case-insensitive and non-ordinal,
                    // while CompareEndpoints uses string.CompareOrdinal. The two disagree, and
                    // the disagreement would reject rows the service considers canonical.
                    //
                    // The group-id tiebreak is equally load-bearing. Comparing only the type
                    // names gives zero protection when both endpoints are the same type —
                    // precisely the BibleReference-to-BibleReference case the design exists
                    // for. Guid comparison here is SQL Server's, which orders by bytes 10-15
                    // first; CompareEndpoints uses SqlGuid for exactly that reason, so the two
                    // agree. A Guid.CompareTo in the service would not.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_Association_CanonicalOrder",
                        sql:
                            $"[{nameof(Association.EntityAType)}] COLLATE Latin1_General_BIN2 " +
                            $"< [{nameof(Association.EntityBType)}] COLLATE Latin1_General_BIN2 " +
                            $"OR ([{nameof(Association.EntityAType)}] COLLATE Latin1_General_BIN2 " +
                            $"= [{nameof(Association.EntityBType)}] COLLATE Latin1_General_BIN2 " +
                            $"AND [{nameof(Association.EntityAGroupId)}] " +
                            $"< [{nameof(Association.EntityBGroupId)}])");
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

            // Uniqueness of the pairing, as a database guarantee rather than a service
            // convention. A foundation service is reachable through a public event address
            // and cannot assume an orchestration deduplicated first, so the constraint has to
            // live here.
            //
            // It keys on the EFFECTIVE id, not the raw ids: two AllVersions rows for the same
            // group differing only in KeyId mean the same thing, and over the raw columns they
            // are distinct rows. The effective id collapses them.
            //
            // UserId goes LAST and stays nullable, which is what lets one index carry two
            // different meanings. SQL Server — unlike the SQL standard — treats NULL as equal
            // to NULL in a unique index, so a null UserId means "exactly one of these
            // globally" (an editorial pairing) while a set value means "exactly one per user"
            // (a reaction). Without the column, the 112th "Amen" on a passage fails with a
            // duplicate key, because Reaction is a lookup row and every reaction association
            // is otherwise byte-identical.
            //
            // The explicit filter also REPLACES the one EF would generate. Left to itself, EF
            // filters a unique index over a nullable column with "WHERE [UserId] IS NOT NULL",
            // which would exempt every editorial row from the uniqueness it most needs.
            //
            // The content types are deliberately absent: they are derived from the endpoint,
            // not part of identity.
            model.HasIndex(association => new
            {
                association.EntityAType,
                association.EntityAEffectiveId,
                association.EntityBType,
                association.EntityBEffectiveId,
                association.UserId
            })
                 .IsUnique()
                 .HasFilter($"[{nameof(Association.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_Associations_Pair");
        }
    }
}
