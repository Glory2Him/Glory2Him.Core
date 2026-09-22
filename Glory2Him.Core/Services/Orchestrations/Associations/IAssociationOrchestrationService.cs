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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    /// <summary>
    /// The top layer of the <c>Association</c> stack, and the one an exposer binds to: §SEC14.3's
    /// composite visibility rule spans both endpoints, so a public read surface must bind here
    /// rather than to the foundation's collection read, and §EVN13 rule 3 binds an approvable
    /// entity's exposer to its top-layer service. Public — unlike the implementation behind it —
    /// because a public controller constructor cannot take an internal parameter type (CS0051);
    /// <c>AssociationOrchestrationService</c> and the outer exception types stay internal and
    /// reach the host through <c>InternalsVisibleTo</c>, per the <c>ITagService</c> /
    /// <c>IApprovalOrchestrationService</c> precedent.
    /// </summary>
    public partial interface IAssociationOrchestrationService
    {
        /// <summary>
        /// Suggests an association between two endpoints — the retrieve-or-add flow of design
        /// §7.4. The caller supplies only the raw endpoints (<c>EntityAType</c>/<c>EntityAKeyId</c>,
        /// <c>EntityBType</c>/<c>EntityBKeyId</c>, and <c>UserId</c> for a reaction); the
        /// orchestration resolves each endpoint against its foundation service and DERIVES the
        /// scope, group id and content type — none of which it accepts from the caller, because
        /// the content type is an authorization input and a caller-set scope could claim
        /// <c>AllVersions</c> on an entity with no group.
        ///
        /// <para>It then looks the canonical pair up over the unfiltered store and branches:
        /// an unoccupied pair is inserted (<c>Created</c>); an occupied one is returned as-is —
        /// <c>AlreadyApproved</c> for an approved row, <c>AlreadyPending</c> for any other
        /// non-deleted state (pending and rejected are indistinguishable to the caller by
        /// design); the caller's own soft-deleted row is resurrected to <c>Draft</c>
        /// (<c>Restored</c>), while a moderator-deleted row is never resurrected, so a takedown
        /// cannot be laundered by resubmitting. The result carries a status and the row id and
        /// NOTHING else — the row body would leak another user's authorship.</para>
        /// </summary>
        ValueTask<AssociationSuggestionResult> AddAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The association collection read, and the first place §SEC14.3's composite is actually
        /// evaluated. It composes rules 3 and 4 — an association is visible only while <b>both</b>
        /// of its endpoints are — above the foundation's self-only filter over rules 1, 2 and 5,
        /// by resolving each endpoint through that endpoint entity's own collection read. The
        /// caller clause therefore lives in the endpoint's read rather than in the composite: a
        /// moderator keeps the pairing on the <c>Submitted</c> item they moderate because that
        /// item's read admits them, and a soft-deleted endpoint drops the pairing for everyone,
        /// <c>Administrators</c> included (§SEC14.5 rule 3).
        ///
        /// <para>A row the caller may not see is <b>absent from the set</b> rather than an error,
        /// and the answer reveals no count of what was dropped (§SEC14.5 rule 4). There is no
        /// gate of its own on this path — a read is filtered, never refused.</para>
        ///
        /// <para>The queryable comes back <b>unenumerated</b> and issues no endpoint round trip
        /// of its own, whatever the row count: the composite composes into the association query
        /// instead of resolving anything above it. Composing it further is the caller's to do.
        /// </para>
        /// </summary>
        ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The single-row read. It denies by <b>not-found</b>, never by unauthorized and never by
        /// a dependency error, and it answers the same way to all four misses: an id that occupies
        /// no row, an id naming a soft-deleted row, an id naming a row the caller may not see
        /// under the foundation's posture, and an id naming a row whose endpoint is not visible
        /// to this caller.
        ///
        /// <para>The last of those resolves its two endpoints directly and reuses the conversion
        /// <c>ResolveEndpointAsync</c> already performs on the add path. Letting it surface as a
        /// dependency failure instead would answer "this endpoint is not visible" with a 424,
        /// which reports a visibility rule as a broken dependency and leaks through the status
        /// code exactly what §SEC14.5 rule 2 keeps out of the message.</para>
        ///
        /// <para>The caller-facing exception carries <b>no reason, no state and no identity</b>
        /// in its message or its <c>Data</c>. The true reason is logged server-side by the layer
        /// that knows it — a warning for a privilege denial, information for a state-based miss
        /// (§SEC14.5 rules 5-7) — immediately before the generic answer is thrown.</para>
        /// </summary>
        ValueTask<Association> RetrieveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The general modify. <b>There is no field map, and looking for one is looking for
        /// something that does not exist.</b> <c>Association</c> has no caller-editable content
        /// at all (§APR7.5.1 rule 4) — every non-audit property is pinned against storage — so
        /// the whole effective payload is <c>ApprovalStatus</c> moving between <c>Draft</c> and
        /// <c>Submitted</c>, the carve-out of §APR9.2 rules 4-6.
        ///
        /// <para>A caller who supplies a changed endpoint, scope, confidence, sort order,
        /// <c>IsPublished</c> or <c>PublishDate</c> gets the stored value back unchanged rather
        /// than an error. A stored <c>Approved</c> or <c>Rejected</c> row refuses the write
        /// outright: an association never forks, so refusing <b>is</b> the enforcement
        /// (§APR7.5.1 rule 3). The carve-out is gated on ownership — the owner, or the
        /// endpoint-derived <c>Publishers</c> tier — and never a reviewer (§APR9.2 rule 4).</para>
        ///
        /// <para><b>This member composes nothing.</b> It runs the half of the gate that needs no
        /// row — authentication and the global <c>ReadOnly</c> block — and forwards. Everything
        /// composed from the stored endpoints, including the <c>Publishers</c> tier and each
        /// end's <c>ReadOnly</c> veto, runs in the foundation beneath it and reaches the caller
        /// as <c>AssociationOrchestrationDependencyValidationException</c>. Adding endpoint
        /// resolution here is a finding (§SEC14.7 posture A′ rule 4).</para>
        /// </summary>
        ValueTask<Association> ModifyAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);
    }
}
