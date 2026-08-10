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
using Glory2Him.Core.Models.Foundations.Comments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddCommentConfigurations(EntityTypeBuilder<Comment> model)
        {
            model.ToTable("Comments");

            model.HasKey(comment => comment.Id);

            model.Property(comment => comment.Id)
                 .IsRequired();

            model.Property(comment => comment.Content)
                 .IsRequired();

            model.Property(comment => comment.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(comment => comment.CreatedWhen)
                 .IsRequired();

            model.Property(comment => comment.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(comment => comment.UpdatedWhen)
                 .IsRequired();

            model.Property(comment => comment.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(comment => comment.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(comment => comment.DeletedWhen)
                 .IsRequired(false);

            model.Property(comment => comment.DeletionReason)
                 .IsRequired(false);

            model.Property(comment => comment.PublishDate)
                 .IsRequired(false);

            model.Property(comment => comment.IsPublished)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(comment => comment.ApprovalStatus)
                 .IsRequired()
                 .HasDefaultValue(ApprovalStatus.Draft);

            model.Property(comment => comment.IsApprovedByBypass)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(comment => comment.ApprovedByBypassReason)
                 .HasMaxLength(500)
                 .IsRequired(false);
        }
    }
}
