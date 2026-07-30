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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
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
                            $"(({nameof(ContentItemAssociation.LinkedContentScope)} = " +
                            $"N'{nameof(Scope.AllVersions)}' AND " +
                            $"{nameof(ContentItemAssociation.LinkedContentItemGroupId)} IS NOT NULL AND " +
                            $"{nameof(ContentItemAssociation.LinkedContentItemId)} IS NULL) OR " +
                            $"({nameof(ContentItemAssociation.LinkedContentScope)} = " +
                            $"N'{nameof(Scope.ThisVersionOnly)}' AND " +
                            $"{nameof(ContentItemAssociation.LinkedContentItemId)} IS NOT NULL AND " +
                            $"{nameof(ContentItemAssociation.LinkedContentItemGroupId)} IS NULL))");
                });

            model.HasKey(contentItemAssociation => contentItemAssociation.Id);

            model.Property(contentItemAssociation => contentItemAssociation.LinkedContentScope)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(contentItemAssociation => contentItemAssociation.LinkedEntityType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired();

            model.Property(contentItemAssociation => contentItemAssociation.LinkedContentItemId)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.LinkedContentItemGroupId)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.LinkedEntityId)
                 .IsRequired();

            model.Property(contentItemAssociation => contentItemAssociation.CreatedBy)
                 .IsRequired()
                 .HasMaxLength(256);

            model.Property(contentItemAssociation => contentItemAssociation.UpdatedBy)
                 .IsRequired()
                 .HasMaxLength(256);

            model.Property(contentItemAssociation => contentItemAssociation.CreatedWhen)
                 .IsRequired();

            model.Property(contentItemAssociation => contentItemAssociation.UpdatedWhen)
                 .IsRequired();

            model.Property(contentItemAssociation => contentItemAssociation.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(contentItemAssociation => contentItemAssociation.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.DeletedWhen)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.DeletionReason)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.PublishDate)
                 .IsRequired(false);

            model.Property(contentItemAssociation => contentItemAssociation.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(contentItemAssociation => contentItemAssociation.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            // By target entity (what this association points to)
            model.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId })
                 .HasDatabaseName("IX_ContentItemAssociation_Target");

            // By scope: all versions of an associated content item group
            model.HasIndex(e => new { e.LinkedContentScope, e.LinkedContentItemGroupId })
                 .HasFilter(
                     $"[{nameof(ContentItemAssociation.LinkedContentScope)}] = N'{nameof(Scope.AllVersions)}'")
                 .HasDatabaseName("IX_ContentItemAssociation_ByAssociatedContentItemGroupId_ScopeAll");

            // By scope: a specific version of an associated content item
            model.HasIndex(e => new { e.LinkedContentScope, e.LinkedContentItemId })
                 .HasFilter(
                     $"[{nameof(ContentItemAssociation.LinkedContentScope)}] = N'{nameof(Scope.ThisVersionOnly)}'")
                 .HasDatabaseName("IX_ContentItemAssociation_ByItem_ScopeThis");
        }
    }
}
