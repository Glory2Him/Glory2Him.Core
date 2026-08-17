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
    /// <summary>
    /// The tag foundation service contract. Public — unlike its sibling foundation interfaces —
    /// because an exposer binds to it: <c>TagsController</c> in the portal host takes it as its
    /// only dependency, and a public controller constructor cannot accept a less-accessible
    /// parameter type. Only the contract is public; <c>TagService</c>, the brokers behind it and
    /// the outer exception types stay internal and reach the host through
    /// <c>InternalsVisibleTo</c>, so the implementation remains Core's to change.
    /// </summary>
    public partial interface ITagService
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
        /// Moves a tag's approval state (design §9.7.1, §8.6). One verb carries every such
        /// move, because they are one act under different authority rather than three
        /// operations: the ordinary <c>Submitted → Approved</c>/<c>Rejected</c> verdict, the
        /// <c>Admin</c> override that re-opens a terminal row, and the bypass that waives the
        /// §8.5 conditions.
        ///
        /// <para>The caller's copy carries the whole of <c>IApproval</c> as a unit — the target
        /// status (<c>Submitted</c>, <c>Approved</c> or <c>Rejected</c>; <c>Draft</c> and
        /// <c>Dismissed</c> are refused) and its publication fields — plus the bypass pair as a
        /// REQUEST. Everything authorization rests on is read from the STORED row instead: the
        /// author, and the status that decides whether this is an ordinary decision or an
        /// override.</para>
        ///
        /// <para>Three values are derived rather than accepted. Publication is forced off for
        /// any target but <c>Approved</c>, so an override cannot leave a re-opened row public.
        /// <c>IsApprovedByBypass</c> is written from the verdict's <c>IsBypassUsed</c>, and the
        /// reason is retained only when a waiver actually occurred (§9.7.1 rule 3, §9.7.5).</para>
        ///
        /// <para>Publishes the fact the DECISION names — <c>Tag-Approved</c>,
        /// <c>Tag-Rejected</c> or <c>Tag-Submitted</c> — never <c>Tag-Modified</c>, which the
        /// approval workflow subscribes to.</para>
        /// </summary>
        ValueTask<Tag> TransitionTagApprovalAsync(
            Tag tag,
            CancellationToken cancellationToken = default);
    }
}
