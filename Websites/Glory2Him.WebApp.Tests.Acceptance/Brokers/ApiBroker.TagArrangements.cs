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
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Tag rows arranged and torn down beneath HTTP, for state no endpoint can produce.
    ///
    /// <para>Split out of <c>ApiBroker.Approvals.cs</c> when the second exposer arrived: the
    /// approval round is shared by every approvable entity and stayed there, while the entity
    /// row is per entity and belongs here. Each exposer adds its own
    /// <c>ApiBroker.&lt;Entity&gt;Arrangements.cs</c> rather than growing the shared file.</para>
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreTag> InsertSubmittedTagAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var tag = new CoreTag
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),
                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertTagAsync(tag);
        }

        public async ValueTask<CoreTag> GetCoreTagByIdAsync(Guid tagId) =>
            await this.storageBroker.SelectTagByIdAsync(tagId);

        public async ValueTask RemoveCoreTagAsync(CoreTag tag) =>
            await this.storageBroker.DeleteTagAsync(tag);

        /// <summary>
        /// Physically removes a tag if it is still there, whatever state it is in. Every
        /// acceptance test finishes with this so the database is left as it was found: the API's
        /// own delete is a SOFT delete, so a test that tore down through the endpoint still left
        /// its row behind, and a test whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreTagByIdAsync(Guid tagId)
        {
            CoreTag storedTag = await this.storageBroker.SelectTagByIdAsync(tagId);

            if (storedTag is not null)
            {
                await this.storageBroker.DeleteTagAsync(storedTag);
            }
        }
    }
}
