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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial interface IContentItemOrchestrationService
    {
        /// <summary>
        /// Adds a new content item as version 1 of a new group (Flow 1). The caller must be
        /// authenticated and not blocked by the <c>ReadOnly</c> or <c>ContentItem-ReadOnly</c>
        /// roles. When the normalized content duplicates an existing non-deleted item of the
        /// same content type (design §3.4.2), nothing is created and the call fails with an
        /// already-exists validation error. On success the orchestration publishes its own
        /// <c>ContentItemOrchestration-Added</c> completion fact, distinct from the
        /// foundation's row-level <c>ContentItem-Added</c>.
        /// </summary>
        ValueTask<ContentItem> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies an existing content item's content properties (Flow 2), branching on the
        /// current <c>ApprovalStatus</c>: a not-yet-approved item is modified in place on the
        /// same row and version by its owner or by a <c>Reviewer</c>, <c>Publisher</c> or
        /// <c>Admin</c>; an <c>Approved</c> item may only be modified by its owner, which forks
        /// a new version row (<c>Version + 1</c>, new row becomes the latest, previous latest
        /// is demoted). Only the permitted caller fields (<c>Title</c>, <c>Author</c>,
        /// <c>Content</c>, <c>ContentTypeId</c>, <c>PublishDate</c>) are mapped onto the entity
        /// loaded from storage, so control fields can never be tampered with; <c>CreatedBy</c>
        /// never changes on an update. State transitions such as approval and publication are
        /// deliberately NOT reachable here — they own narrower field scopes and get their own
        /// methods. Duplicate content in another group (design §3.4.2) fails with an
        /// already-exists validation error. A single <c>ContentItemOrchestration-Modified</c>
        /// completion fact is published once the work has landed, including for a fork, where
        /// two foundation rows are written but the process completed once.
        /// </summary>
        ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an existing content item (Flow 3 — the soft delete of design §10.4). The
        /// caller must be authenticated, not blocked, and either the item's owner
        /// (<c>CreatedBy</c>) or an <c>Admin</c>. The control fields (<c>IsDeleted</c>,
        /// <c>DeletedBy</c>, <c>DeletedWhen</c>, <c>DeletionReason</c>) are set internally by
        /// the foundation service, never accepted from the caller; <c>ApprovalStatus</c> is
        /// deliberately left untouched because deletion is not part of the approval workflow
        /// (§10.5). An already-removed item is reported as not found, and a removed item drops
        /// out of the duplicate-content rule so its wording may be added again. On success the
        /// orchestration publishes <c>ContentItemOrchestration-Removed</c>.
        /// </summary>
        ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a single content item version by its <c>Id</c>, enforcing the read
        /// posture of design §14.1/§16.6: a version that satisfies canonical content
        /// visibility (not deleted, <c>Approved</c>, <c>IsPublished</c>, and
        /// <c>PublishDate</c> null or past) is readable by anyone — reads carry no
        /// contribution gate, so anonymous and even <c>ReadOnly</c>-blocked callers may
        /// read public content. A non-public version (<c>Draft</c>, <c>Submitted</c>,
        /// <c>Rejected</c>, <c>Dismissed</c>, unpublished, or scheduled in the future) is
        /// readable only by its owner (<c>CreatedBy</c>) or a <c>Reviewer</c>,
        /// <c>Publisher</c> or <c>Admin</c> (global or ContentItem-scoped) for review and
        /// audit; every other caller receives not-found — never unauthorized — so an
        /// unprivileged probe cannot tell a non-public version from a missing one. A
        /// soft-deleted row is not found for every caller. Being a read, no completion
        /// fact is published.
        /// </summary>
        ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);
    }
}
