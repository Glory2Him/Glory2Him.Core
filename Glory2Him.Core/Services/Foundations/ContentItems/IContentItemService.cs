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
using Glory2Him.Core.Models.Events;
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

        /// <summary>
        /// Clears the group's published slot so a newly approved sibling can take it
        /// (design §9.7.7 rules 6–7). Owns <c>IsPublished</c> and
        /// <c>PublishDate</c> and nothing else — the row stays <c>Approved</c>,
        /// because it was approved; it is superseded, not un-approved — and
        /// <c>IsLatestVersion</c> is untouched (§3.4 rule 18).
        ///
        /// <para>Gated to <c>Admin</c> or the workflow's system identity, never the
        /// publisher tier: the row being unpublished is itself <c>Approved</c>, and
        /// §8.6 HR-4 bars a <c>Publisher</c> from moving an approved row.</para>
        ///
        /// <para>Deliberately loads a soft-deleted row too. The published slot is held
        /// by an index filtered on <c>IsPublished</c> alone, so a tombstone that kept
        /// the flag still occupies it — refusing to clear one would make the group
        /// permanently unpublishable.</para>
        /// </summary>
        ValueTask<ContentItem> UnpublishContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The publication swap's route into <see cref="UnpublishContentItemByIdAsync(System.Guid,
        /// System.Threading.CancellationToken)"/>, taking the envelope the swap is acting
        /// under so the workflow's identity is CARRIED rather than re-asserted.
        ///
        /// <para>Minting a fresh context here would read the ambient HTTP caller — who on
        /// an automatic approval is the reviewer whose own review completed the round, not
        /// an <c>Admin</c> — and the unpublish would be refused for the one caller entitled
        /// to make it.</para>
        /// </summary>
        internal ValueTask<ContentItem> UnpublishContentItemByIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The publication swap's second write, taking the envelope the swap is acting
        /// under so the workflow's identity is CARRIED rather than re-asserted — the same
        /// reason <see cref="UnpublishContentItemByIdAsync(System.Guid,
        /// Glory2Him.Core.Models.Events.EventEnvelope{ContentItem}, System.Threading.CancellationToken)"/>
        /// takes one.
        ///
        /// <para>Minting a fresh context here would read the ambient caller and re-ask a
        /// question already answered on the <c>Approval</c> row. It fails deterministically:
        /// the decision function refuses any outcome once the approval is no longer
        /// <c>Submitted</c>, which by this point it is not (§16.7.1).</para>
        /// </summary>
        internal ValueTask<ContentItem> TransitionContentItemApprovalAsync(
            ContentItem contentItem,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The id of the row currently holding this group's published slot, if any, over the
        /// UNFILTERED store — or <c>null</c> when the slot is free.
        ///
        /// <para>The publication swap cannot use the caller-facing collection read: that applies
        /// the visibility filter, which drops soft-deleted rows. A soft delete never clears
        /// <c>IsPublished</c> and the slot index names that column alone, so a tombstone still
        /// occupies the slot while being invisible to every ordinary read. A filtered probe would
        /// report no incumbent, the demote would be skipped, and the promote would be refused by
        /// the unique index — permanently, for every future approval in the group
        /// (design §9.7.7 rule 7).</para>
        ///
        /// <para>Only an id crosses back, following the §14.6 pattern of filtered reads for
        /// entities and gated probes for cross-row facts.</para>
        /// </summary>
        ValueTask<Guid?> FindPublishedContentItemIdByGroupAsync(
            Guid groupId,
            Guid excludedContentItemId,
            CancellationToken cancellationToken = default);
    }
}
