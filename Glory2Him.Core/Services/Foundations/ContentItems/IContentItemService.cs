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
        /// the <c>IAccessBroker</c> decision — no self-approval (HR-2), never a reviewer (HR-3)
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
        /// <para>Gated to <c>Administrators</c> or the workflow's system identity, never the
        /// publisher tier: the row being unpublished is itself <c>Approved</c>, and
        /// §8.6 HR-4 bars a publisher from moving an approved row.</para>
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
        /// an administrator — and the unpublish would be refused for the one caller entitled
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
        /// The id of the row holding the published slot of <paramref name="contentItemId"/>'s own
        /// group, excluding that row itself — or <c>null</c> when the slot is free. The
        /// publication swap's single probe: it resolves the group and the incumbent in one
        /// gated call, over the UNFILTERED store.
        ///
        /// <para>The swap cannot resolve the group through
        /// <see cref="RetrieveContentItemByIdAsync(System.Guid, System.Threading.CancellationToken)"/>.
        /// That read is caller-FILTERED, and the swap acts on the workflow's system identity,
        /// which carries no roles and is not the row's owner — so the filtered read answers
        /// not-found for the one actor entitled to run the swap. Taking the group off a
        /// caller-supplied payload instead would let one group's approval unpublish another
        /// group's live row.</para>
        ///
        /// <para>A gated probe rather than a filtered read, following the §14.6 pattern:
        /// filtered reads for entities, gated probes for the single cross-row facts a write
        /// flow needs. Only an id crosses back, to a caller already admitted to contribute —
        /// revealing nothing they could not infer from the group having a published version.</para>
        ///
        /// <para>Deliberately does not drop soft-deleted incumbents: a tombstone that kept
        /// <c>IsPublished</c> still occupies the slot (§9.7.7 rule 7). A missing or
        /// soft-deleted TARGET, by contrast, answers not-found — a tombstone has no group
        /// membership to promote into.</para>
        /// </summary>
        internal ValueTask<Guid?> FindPublishedSiblingContentItemIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The highest <c>Version</c> any row of this group has ever held, over the UNFILTERED
        /// store — <c>0</c> when the group has no rows.
        ///
        /// <para>Deliberately distinct from "which row is the tip". The tip is the highest LIVE
        /// version, because nobody edits a tombstone. The next free version number must account
        /// for tombstones too, because the unique index on <c>(GroupId, Version)</c> carries no
        /// <c>IsDeleted</c> filter and a soft-deleted row still owns its number.</para>
        ///
        /// <para>Conflating the two was issue #271: a live row beneath a soft-deleted higher
        /// version looked like the tip, and the fork numbered its successor onto the tombstone,
        /// failing at the index for every subsequent fork in that group.</para>
        /// </summary>
        ValueTask<int> FindHighestVersionInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

    }
}
