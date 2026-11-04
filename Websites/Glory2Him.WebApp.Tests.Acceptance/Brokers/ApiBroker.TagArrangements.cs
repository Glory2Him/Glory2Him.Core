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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
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
        public async ValueTask<CoreTag> InsertSubmittedTagAsync(string authorUserId) =>
            await InsertTagAtAsync(authorUserId, ApprovalStatus.Submitted);

        /// <summary>
        /// A DRAFT, for the submit verb to have something to move. Beneath HTTP because the
        /// endpoint's own add is what opens the round, and the point of the test that uses this
        /// is a round that was never opened.
        /// </summary>
        public async ValueTask<CoreTag> InsertDraftTagAsync(string authorUserId) =>
            await InsertTagAtAsync(authorUserId, ApprovalStatus.Draft);

        /// <summary>
        /// A REJECTED tag, whose name a suggestion withholds rather than pairs. Beneath HTTP
        /// because no endpoint rejects a tag: rejection arrives as an event.
        /// </summary>
        public async ValueTask<CoreTag> InsertRejectedTagAsync(string authorUserId) =>
            await InsertTagAtAsync(authorUserId, ApprovalStatus.Rejected);

        /// <summary>
        /// An approved, published tag, visible to every caller. Beneath HTTP because no endpoint
        /// publishes a tag: approval arrives as an event.
        /// </summary>
        public async ValueTask<CoreTag> InsertPublishedTagAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var tag = new CoreTag
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
                PublishDate = now,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertTagAsync(tag);
        }

        private async ValueTask<CoreTag> InsertTagAtAsync(
            string authorUserId,
            ApprovalStatus approvalStatus)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var tag = new CoreTag
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),
                ApprovalStatus = approvalStatus,
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

        /// <summary>
        /// Every tag holding the name, at any status, live or soft-deleted. A tag suggestion adds
        /// a tag the response never names by its id, so a test finds it by the name it posted.
        /// </summary>
        public async ValueTask<IReadOnlyList<CoreTag>> GetCoreTagsByNameAsync(string name) =>
            await this.storageBroker.SelectTagsAsync(
                tags => tags.Where(tag => tag.Name == name));

        /// <summary>
        /// Physically removes every tag holding the name, with the approval round each one's
        /// write opened: a tag a suggestion adds is written at <c>Submitted</c>, so the approval
        /// workflow opens a round on it as the request is served.
        /// </summary>
        public async ValueTask RemoveCoreTagsByNameAsync(string name)
        {
            IReadOnlyList<CoreTag> storedTags = await GetCoreTagsByNameAsync(name);

            foreach (CoreTag storedTag in storedTags)
            {
                Approval storedApproval =
                    await GetCoreApprovalByEntityAsync(EntityType.Tag, storedTag.Id);

                if (storedApproval is not null)
                {
                    await this.storageBroker.DeleteApprovalAsync(storedApproval);
                }

                await this.storageBroker.DeleteTagAsync(storedTag);
            }
        }

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
