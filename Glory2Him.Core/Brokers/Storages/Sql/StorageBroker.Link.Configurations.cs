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
using Glory2Him.Core.Models.Foundations.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddLinkConfigurations(EntityTypeBuilder<Link> model)
        {
            model.ToTable("Links");

            model.HasKey(link => link.Id);

            model.Property(link => link.Id)
                 .IsRequired();

            model.Property(link => link.Name)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(link => link.Url)
                 .HasMaxLength(2048)
                 .IsRequired();

            model.Property(link => link.LinkType)
                 .HasMaxLength(64)
                 .IsRequired();

            model.Property(link => link.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(link => link.CreatedWhen)
                 .IsRequired();

            model.Property(link => link.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(link => link.UpdatedWhen)
                 .IsRequired();

            model.Property(link => link.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(link => link.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(link => link.DeletedWhen)
                 .IsRequired(false);

            model.Property(link => link.DeletionReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            model.Property(link => link.GroupId)
                 .IsRequired();

            model.Property(link => link.Version)
                 .IsRequired()
                 .HasDefaultValue(1);

            model.Property(link => link.IsLatestVersion)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(link => link.PublishDate)
                 .IsRequired(false);

            model.Property(link => link.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(link => link.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            model.Property(link => link.IsApprovedByBypass)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(link => link.ApprovedByBypassReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            // Unique version per group
            model.HasIndex(link => new { link.GroupId, link.Version })
                 .IsUnique()
                 .HasDatabaseName("UX_Links_GroupId_Version");

            // Exactly one latest version per group
            model.HasIndex(link => new { link.GroupId, link.IsLatestVersion })
                 .IsUnique()
                 .HasFilter($"[{nameof(Link.IsLatestVersion)}] = 1")
                 .HasDatabaseName("UX_Links_GroupId_IsLatestVersion");

            // Exactly one published version per group
            model.HasIndex(link => new { link.GroupId, link.IsPublished })
                 .IsUnique()
                 .HasFilter($"[{nameof(Link.IsPublished)}] = 1")
                 .HasDatabaseName("UX_Links_GroupId_IsPublished");
        }
    }
}
