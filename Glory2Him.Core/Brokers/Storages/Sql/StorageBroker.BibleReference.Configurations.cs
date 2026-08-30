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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddBibleReferenceConfigurations(EntityTypeBuilder<BibleReference> model)
        {
            model.ToTable("BibleReferences");

            model.HasKey(bibleReference => bibleReference.Id);

            model.Property(bibleReference => bibleReference.Id)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.USFM)
                 .HasMaxLength(50)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Reference)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Translation)
                 .HasMaxLength(50)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.Scripture)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.ScriptureHtml)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.CreatedWhen)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.UpdatedWhen)
                 .IsRequired();

            model.Property(bibleReference => bibleReference.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.DeletedWhen)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.DeletionReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.PublishDate)
                 .IsRequired(false);

            model.Property(bibleReference => bibleReference.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            model.Property(bibleReference => bibleReference.IsApprovedByBypass)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(bibleReference => bibleReference.ApprovedByBypassReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            // BibleReference is a Single-Row entity (no IVersion) — USFM is the canonical
            // passage key (includes translation, since Scripture is translation-specific)
            // and is immutable after creation (pinned in ValidateAgainstStorageBibleReferenceOnModify).
            model.HasIndex(bibleReference => bibleReference.USFM)
                 .IsUnique()
                 .HasFilter($"[{nameof(BibleReference.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_BibleReferences_USFM");
        }
    }
}
