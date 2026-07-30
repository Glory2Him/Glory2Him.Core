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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial interface IContentItemOrchestrationService
    {
        /// <summary>
        /// Submits a new content item as version 1 of a new group (Flow 1 — Add). The caller
        /// must be authenticated and not blocked by the <c>ReadOnly</c> or
        /// <c>ContentItem-ReadOnly</c> roles. When the normalized content duplicates an
        /// existing non-deleted item of the same content type (design §3.4.2), nothing is
        /// created and the submission fails with an already-exists validation error.
        /// </summary>
        ValueTask<ContentItem> SubmitContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies an existing content item (Flow 2 — Modify), branching on the current
        /// <c>ApprovalStatus</c>: a not-yet-approved item is amended in place on the same row
        /// and version; an <c>Approved</c> item edited by its owner forks a new version row
        /// (<c>Version + 1</c>, new row becomes the latest, previous latest is demoted); an
        /// <c>Approved</c> item amended by an <c>Admin</c> is changed in place without a fork.
        /// A <c>Publisher</c> or <c>Admin</c> may amend a <c>Submitted</c> item during review.
        /// Only the permitted caller fields (<c>Title</c>, <c>Author</c>, <c>Content</c>,
        /// <c>ContentTypeId</c>, <c>PublishDate</c>) are mapped onto the entity loaded from
        /// storage, so control fields can never be tampered with; <c>CreatedBy</c> never
        /// changes on an update. Duplicate content in another group (design §3.4.2) fails
        /// with an already-exists validation error.
        /// </summary>
        ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);
    }
}
