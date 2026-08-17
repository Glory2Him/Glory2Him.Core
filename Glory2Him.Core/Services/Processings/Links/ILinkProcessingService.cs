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
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial interface ILinkProcessingService
    {
        /// <summary>
        /// Adds a new link as version 1 of a new group (design §3.3, §3.4). The caller must be
        /// authenticated and not blocked by the <c>ReadOnly</c> or <c>Link-ReadOnly</c> roles.
        /// On success this service publishes its own <c>LinkProcessing-Added</c> completion
        /// fact, distinct from the foundation's row-level <c>Link-Added</c>.
        ///
        /// <para>Unlike <c>ContentItem</c> there is no duplicate-content rule here: the
        /// §3.4.2 rule is keyed on (<c>ContentType</c>, <c>ContentHash</c>), and a link
        /// carries neither. Two links to the same URL are a legitimate pair — the same
        /// article cited from two stories, under two names.</para>
        /// </summary>
        ValueTask<Link> AddLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies an existing link's content properties, branching on the current
        /// <c>ApprovalStatus</c>: a non-terminal item (<c>Draft</c> or <c>Submitted</c>) is
        /// modified in place on the same row and version by its owner or by a
        /// <c>Reviewer</c>, <c>Publisher</c> or <c>Admin</c>; a terminal item
        /// (<c>Approved</c> or <c>Rejected</c>) may only be modified by its owner, which
        /// forks a new version row (<c>Version + 1</c>, new row becomes the latest, previous
        /// latest is demoted). Only the permitted caller fields (<c>Name</c>, <c>Url</c>,
        /// <c>LinkType</c>) are mapped onto the entity loaded from storage, so control fields
        /// can never be tampered with; <c>CreatedBy</c> never changes on an update. State
        /// transitions such as approval and publication are deliberately NOT reachable here —
        /// they own narrower field scopes and get their own methods. A single
        /// <c>LinkProcessing-Modified</c> completion fact is published once the work has
        /// landed, including for a fork, where two foundation rows are written but the
        /// process completed once.
        /// </summary>
        ValueTask<Link> ModifyLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an existing link (the soft delete of design §10.4). The caller must be
        /// authenticated, not blocked, and either the link's owner (<c>CreatedBy</c>) or an
        /// <c>Admin</c>. The control fields (<c>IsDeleted</c>, <c>DeletedBy</c>,
        /// <c>DeletedWhen</c>, <c>DeletionReason</c>) are set internally by the foundation
        /// service, never accepted from the caller; <c>ApprovalStatus</c> is deliberately
        /// left untouched because deletion is not part of the approval workflow (§10.5). An
        /// already-removed link is reported as not found. On success this service publishes
        /// <c>LinkProcessing-Removed</c>.
        /// </summary>
        ValueTask<Link> RemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a single link version by its <c>Id</c>, enforcing the read posture of
        /// design §14.1/§16.6: a version that satisfies canonical content visibility (not
        /// deleted, <c>Approved</c>, <c>IsPublished</c>, and <c>PublishDate</c> null or past)
        /// is readable by anyone — reads carry no contribution gate, so anonymous and even
        /// <c>ReadOnly</c>-blocked callers may read public content. A non-public version
        /// (<c>Draft</c>, <c>Submitted</c>, <c>Rejected</c>, <c>Dismissed</c>, unpublished,
        /// or scheduled in the future) is readable only by its owner (<c>CreatedBy</c>) or a
        /// <c>Reviewer</c>, <c>Publisher</c> or <c>Admin</c> (global or Link-scoped) for
        /// review and audit; every other caller receives not-found — never unauthorized — so
        /// an unprivileged probe cannot tell a non-public version from a missing one. A
        /// soft-deleted row is not found for every caller. Every denial is logged server-side
        /// with its true reason before the reason-free error is thrown (§14.5) — the reason
        /// never travels outward on the exception. Being a read, no completion fact is
        /// published.
        /// </summary>
        ValueTask<Link> RetrieveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all link versions the caller is allowed to see, as a further composable
        /// query — the general read backing admin, moderation and work-queue surfaces.
        /// Soft-deleted rows are excluded for every caller. An anonymous caller sees only
        /// versions that satisfy canonical content visibility (design §14.1: <c>Approved</c>,
        /// <c>IsPublished</c>, and <c>PublishDate</c> null or past); an authenticated caller
        /// additionally sees their own versions in any state; a <c>Reviewer</c>,
        /// <c>Publisher</c> or <c>Admin</c> (global or Link-scoped, §16.6) sees every
        /// non-deleted version for review and audit. Public-facing surfaces should prefer
        /// <see cref="RetrieveAllPublicLinksAsync"/>, which never widens with the caller's
        /// privileges. Being a read, no completion fact is published.
        /// </summary>
        ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves exactly the canonically visible link versions (design §14.1: not
        /// deleted, <c>Approved</c>, <c>IsPublished</c>, and <c>PublishDate</c> null or
        /// past), as a further composable query. The read is caller-independent: no security
        /// context is consulted, so even a privileged caller receives the same set an
        /// anonymous visitor would — the safe default for public-facing surfaces, where
        /// <see cref="RetrieveAllLinksAsync"/> would widen the set with the caller's own or
        /// reviewable rows. Being a read, no completion fact is published.
        /// </summary>
        ValueTask<IQueryable<Link>> RetrieveAllPublicLinksAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all versions of a link group (design §15.1 <c>/groups/{groupId}</c>),
        /// applying the same per-caller visibility filter as
        /// <see cref="RetrieveAllLinksAsync"/>: deleted rows are gone for everyone, anonymous
        /// callers see only publicly visible versions, owners also see their own, and the
        /// review roles see every non-deleted version of the group.
        /// </summary>
        ValueTask<IQueryable<Link>> RetrieveLinksByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the single latest version (<c>IsLatestVersion</c>) of a link group — the
        /// edit tip, which may still be an unapproved draft. The read posture matches
        /// <see cref="RetrieveLinkByIdAsync"/>: a publicly visible latest version is readable
        /// by anyone; a non-public one only by its owner or a <c>Reviewer</c>,
        /// <c>Publisher</c> or <c>Admin</c>; every other caller receives not-found — never
        /// unauthorized — so an unprivileged probe cannot tell a non-public tip from a
        /// missing group. A group with no non-deleted latest version is not found for every
        /// caller. Every denial is logged server-side with its true reason before the
        /// reason-free error is thrown (§14.5).
        /// </summary>
        ValueTask<Link> RetrieveLatestLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the single published version (<c>IsPublished</c>) of a link group — the
        /// row the public currently reads, which stays published while a newer draft is in
        /// review (§3.4.1). The read posture matches <see cref="RetrieveLinkByIdAsync"/>:
        /// when the published row is publicly visible anyone may read it; a published row
        /// scheduled in the future (<c>PublishDate</c> not yet passed) is readable only by
        /// its owner or a <c>Reviewer</c>, <c>Publisher</c> or <c>Admin</c>; everyone else
        /// receives not-found, as does every caller when the group has no non-deleted
        /// published row. Every denial is logged server-side with its true reason before the
        /// reason-free error is thrown (§14.5).
        /// </summary>
        ValueTask<Link> RetrievePublishedLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default);
    }
}
