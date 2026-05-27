// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Comments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker
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
        }
    }
}
