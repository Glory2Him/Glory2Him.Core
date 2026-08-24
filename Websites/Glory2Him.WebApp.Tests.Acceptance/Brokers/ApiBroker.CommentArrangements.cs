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

using System;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using CoreComment = Glory2Him.Core.Models.Foundations.Comments.Comment;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Comment rows arranged and torn down beneath HTTP, for state no endpoint can produce —
    /// the sibling of <c>ApiBroker.TagArrangements.cs</c> and
    /// <c>ApiBroker.ReactionArrangements.cs</c>, and the same shape for the same reasons.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreComment> InsertSubmittedCommentAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var comment = new CoreComment
            {
                Id = Guid.NewGuid(),

                // Required and uncapped. Comment carries no unique index and no length rule,
                // so unlike the Tag and Reaction arrangements nothing here has to avoid a key
                // collision — the guid is for readability in a failure, not for uniqueness.
                Content = $"Arranged by the acceptance suite {Guid.NewGuid():N}",

                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertCommentAsync(comment);
        }

        public async ValueTask<CoreComment> GetCoreCommentByIdAsync(Guid commentId) =>
            await this.storageBroker.SelectCommentByIdAsync(commentId);

        public async ValueTask RemoveCoreCommentAsync(CoreComment comment) =>
            await this.storageBroker.DeleteCommentAsync(comment);

        /// <summary>
        /// Physically removes a comment if it is still there, whatever state it is in. Every
        /// acceptance test finishes with this so the database is left as it was found: the API's
        /// own delete is a SOFT delete, so a test that tore down through the endpoint still left
        /// its row behind, and a test whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreCommentByIdAsync(Guid commentId)
        {
            CoreComment storedComment =
                await this.storageBroker.SelectCommentByIdAsync(commentId);

            if (storedComment is not null)
            {
                await this.storageBroker.DeleteCommentAsync(storedComment);
            }
        }
    }
}
