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
using CoreReaction = Glory2Him.Core.Models.Foundations.Reactions.Reaction;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Reaction rows arranged and torn down beneath HTTP, for state no endpoint can produce —
    /// the sibling of <c>ApiBroker.TagArrangements.cs</c>, and the same shape for the same
    /// reasons.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreReaction> InsertSubmittedReactionAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var reaction = new CoreReaction
            {
                Id = Guid.NewGuid(),

                // IX_Reactions_Name is unique and NOT filtered on IsDeleted (#201), so a name
                // reused across runs would be reserved by whichever soft-deleted row got there
                // first. A fresh guid per arrangement is what keeps that defect out of this
                // suite's results — the tests that mean to exercise it say so.
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),

                // Required, capped at 16 (ReactionService.Validations). A literal emoji would
                // put a surrogate pair in a test fixture for no gain, so this is an ASCII stand
                // in that satisfies the same rules.
                UnicodeEmoji = Guid.NewGuid().ToString("N").Substring(0, 8),

                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertReactionAsync(reaction);
        }

        public async ValueTask<CoreReaction> GetCoreReactionByIdAsync(Guid reactionId) =>
            await this.storageBroker.SelectReactionByIdAsync(reactionId);

        public async ValueTask RemoveCoreReactionAsync(CoreReaction reaction) =>
            await this.storageBroker.DeleteReactionAsync(reaction);

        /// <summary>
        /// Physically removes a reaction if it is still there, whatever state it is in. Every
        /// acceptance test finishes with this so the database is left as it was found: the API's
        /// own delete is a SOFT delete, so a test that tore down through the endpoint still left
        /// its row behind, and a test whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreReactionByIdAsync(Guid reactionId)
        {
            CoreReaction storedReaction =
                await this.storageBroker.SelectReactionByIdAsync(reactionId);

            if (storedReaction is not null)
            {
                await this.storageBroker.DeleteReactionAsync(storedReaction);
            }
        }
    }
}
