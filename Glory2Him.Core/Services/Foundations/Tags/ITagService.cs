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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial interface ITagService
    {
        ValueTask<Tag> AddTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Tag>> RetrieveAllTagsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Tag> RetrieveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);

        ValueTask<Tag> ModifyTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default);

        ValueTask<Tag> RemoveTagByIdAsync(
            Guid tagId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Tag> HardRemoveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a tag's approval status Draft → Submitted (design §9.7.1). A narrow
        /// transition owning only <c>ApprovalStatus</c>: it decides against the STORED row,
        /// admits the owner or the publisher tier (the same set the §9.2 modify carve-out
        /// admits), refuses a row that is not in Draft, and publishes <c>Tag-Submitted</c> —
        /// never <c>Tag-Modified</c>, which the approval workflow subscribes to.
        /// </summary>
        ValueTask<Tag> SubmitTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decides a submitted tag (design §9.7.1, §8.6). The publisher-tier gate and the
        /// <c>IAccessBroker</c> decision — no self-approval (HR-2), never a Reviewer (HR-3) —
        /// are taken against the STORED row; the caller's copy carries only the outcome
        /// (<c>Approved</c> or <c>Rejected</c>) and its publication fields. The two bypass
        /// members are derived from the decision, never accepted. Publishes the fact the
        /// DECISION names: <c>Tag-Approved</c> or <c>Tag-Rejected</c>.
        /// </summary>
        ValueTask<Tag> ApproveTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default);
    }
}
