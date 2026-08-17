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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial interface IContentItemService
    {
        ValueTask<ContentItem> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Answers whether any non-deleted content item of the given type already carries
        /// the given content hash, optionally ignoring one group (the duplicate-content
        /// rule of design §3.4.2). Deliberately computed over the UNFILTERED store —
        /// unlike the entity-returning reads, this returns only a boolean, which reveals
        /// no row data beyond what the duplicate rule already reveals to submitters — so
        /// the rule stays global under the per-caller visibility filtering of §14.6.
        /// Requires a caller allowed to contribute; the probe exists to support the
        /// contribution flows.
        /// </summary>
        ValueTask<bool> CheckContentItemContentExistsAsync(
            ContentType contentType,
            string contentHash,
            Guid? excludedGroupId = null,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> HardRemoveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a content item's approval status Draft → Submitted (design §9.7.1). A narrow
        /// transition owning only <c>ApprovalStatus</c>: it decides against the STORED row,
        /// admits the owner or the publisher tier (the same set the §9.2 modify carve-out
        /// admits), refuses a row that is not in Draft, and publishes <c>ContentItem-Submitted</c>
        /// — never <c>ContentItem-Modified</c>, which the approval workflow subscribes to.
        /// </summary>
        ValueTask<ContentItem> SubmitContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decides a submitted content item (design §9.7.1, §8.6). The publisher-tier gate and
        /// the <c>IAccessBroker</c> decision — no self-approval (HR-2), never a Reviewer (HR-3)
        /// — are taken against the STORED row; the caller's copy carries only the outcome
        /// (<c>Approved</c> or <c>Rejected</c>) and its publication fields. The two bypass
        /// members are derived from the decision, never accepted. Publishes the fact the
        /// DECISION names: <c>ContentItem-Approved</c> or <c>ContentItem-Rejected</c>.
        /// </summary>
        ValueTask<ContentItem> TransitionContentItemApprovalAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);
    }
}
