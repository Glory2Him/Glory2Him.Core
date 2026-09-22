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
        /// <para><b>Which row is asked about is decided by the endpoint's scope.</b> An endpoint
        /// written <c>AllVersions</c> is answered at its <b>group</b> — §DOM4.6 rule 1 makes the
        /// effective id the read predicate, and such an association belongs to the group rather
        /// than to the version current when it was written — and one written
        /// <c>ThisVersionOnly</c> at its row. <see cref="RetrieveAssociationByIdAsync"/> answers
        /// the same question the same way: two resolvers are allowed, two predicates are
        /// not.</para>
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
        /// a dependency error, and it answers <b>five</b> misses: an id that occupies no row; an
        /// id naming a soft-deleted row; an id naming a row the caller may not see under the
        /// foundation's posture; an id naming a row whose endpoint is not visible to this caller;
        /// and an id naming a row whose endpoint is of a type with <b>no foundation service</b>
        /// (<c>Attachment</c>, <c>Association</c>).
        ///
        /// <para>The fourth resolves its two endpoints directly and reuses the conversion
        /// <c>ResolveEndpointAsync</c> already performs on the add path. Letting it surface as a
        /// dependency failure instead would answer "this endpoint is not visible" with a 424,
        /// which reports a visibility rule as a broken dependency and leaks through the status
        /// code exactly what §SEC14.5 rule 2 keeps out of the message.</para>
        ///
        /// <para>The fifth is a not-found <b>here</b> and an ordinary validation failure on the
        /// add, and the difference is who supplied the value: there the caller named the endpoint
        /// type, here they supplied only an association id, so naming it back would report one of
        /// the row's columns. It is also the right answer rather than only the safe wording — no
        /// read can show such an endpoint visible, so §SEC14.3 rule 4 cannot be satisfied and an
        /// undecidable visibility input fails closed.</para>
        ///
        /// <para><b>Which row is asked about follows the endpoint's scope, and matches the
        /// collection read term for term</b> (§SEC14.3: two resolvers are allowed, two predicates
        /// are not). A <c>ContentItem</c> or <c>Link</c> endpoint written <c>AllVersions</c> is
        /// answered at its <b>group</b>, through the group-keyed read its own foundation already
        /// exposes; everything else is answered at its row. Resolving on the key id for every
        /// type puts a row in the list that is not found when it is opened.</para>
        ///
        /// <para>Every one of the five carries the <b>same outward message</b> — the foundation's
        /// own not-found wording — with no reason, no state and no identity in it or in
        /// <c>Data</c>. The two exception families stay two, because they record which layer
        /// refused: misses 1-3 leave as
        /// <c>AssociationOrchestrationDependencyValidationException</c> and misses 4-5 as
        /// <c>AssociationOrchestrationValidationException</c>, and an exposer maps both to one
        /// status code and one body. The true reason is logged server-side by the layer that
        /// knows it — a warning for a privilege denial, information for a state-based miss
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
        /// <para><b>Pinned means REFUSED, not absorbed.</b> A caller who supplies a
        /// <b>changed</b> endpoint type, key id or group id, endpoint content type,
        /// <c>EntityAScope</c> / <c>EntityBScope</c>, <c>SortOrder</c>, any <c>IConfidence</c>
        /// field, <c>UserId</c>, <c>IsPublished</c>, <c>PublishDate</c> or the bypass pair is
        /// <b>rejected</b> — one rule per field into <c>InvalidAssociationException</c>, each
        /// offending field named in the exception's <c>Data</c>, reaching the caller through this
        /// service as <c>AssociationOrchestrationDependencyValidationException</c>. Echoing a
        /// stored value back unchanged is not a change and passes. A stored <c>Approved</c> or
        /// <c>Rejected</c> row refuses the write outright: an association never forks, so
        /// refusing <b>is</b> the enforcement (§APR7.5.1 rule 3). The carve-out is gated on
        /// ownership — the owner, or the endpoint-derived <c>Publishers</c> tier — and never a
        /// reviewer (§APR9.2 rule 4).</para>
        ///
        /// <para><b>This member composes nothing.</b> It runs the half of the gate that needs no
        /// row — authentication and the global <c>ReadOnly</c> block — and forwards. Adding
        /// endpoint resolution here is a finding (§SEC14.7 posture A′ rule 4). Beneath it the
        /// foundation composes the endpoint-derived <c>Publishers</c> tier and the endpoint pin
        /// from the <b>stored</b> row, and both reach the caller as
        /// <c>AssociationOrchestrationDependencyValidationException</c>.</para>
        ///
        /// <para><b>The four <c>ReadOnly</c> names are the exception, and on this path today they
        /// are composed from the caller's copy</b> before the storage read — not from the stored
        /// row as §SEC14.7 posture A rule 1 requires and as remove and hard remove already do.
        /// No write a stored-row veto would have refused is admitted, because the pin above holds
        /// all eight endpoint fields against storage; what differs is the refusal's identity — a
        /// sanctioned caller is refused by the pin as an invalid-field failure rather than by the
        /// veto as an unauthorized one. It is a foundation change on behaviour that predates this
        /// surface, tracked as #658, and no member here moves with it. §SEC14.7 posture A′
        /// rule 4 carries both the rule and the gap.</para>
        /// </summary>
        ValueTask<Association> ModifyAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The reversible takedown. The gate splits by layer exactly as modify's does: this layer
        /// runs authentication and the global <c>ReadOnly</c> block and refuses an anonymous or
        /// globally blocked caller <b>unauthorized</b> — a write denial, not a not-found — with
        /// <b>no read against the <c>Associations</c> table at all</b>, so the surface cannot be
        /// used to probe which association ids exist (§SEC14.7 posture A′ rule 4).
        ///
        /// <para>The owner-or-<c>Administrators</c> test, checked before the idempotent
        /// already-deleted short-circuit, and both ends of the <c>ReadOnly</c> veto are composed
        /// from the stored row and belong to the foundation; their refusals arrive as
        /// <c>AssociationOrchestrationDependencyValidationException</c>. This member composes
        /// nothing and issues no second read to duplicate them.</para>
        /// </summary>
        ValueTask<Association> RemoveAssociationByIdAsync(
            Guid associationId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The irreversible deletion, and the one surface that is <c>Administrators</c> alone.
        /// That check is decidable with no row, so it joins authentication and the global
        /// <c>ReadOnly</c> block in this layer's half of the gate: a caller who is not an
        /// <c>Administrators</c> is refused <b>before any read</b>, with the orchestration's own
        /// validation exception.
        ///
        /// <para>The endpoint veto is <b>not</b> here — it is composed from the stored row and
        /// stays in the foundation, where it refuses even an administrator and arrives as
        /// <c>AssociationOrchestrationDependencyValidationException</c>. A block that stopped the
        /// reversible takedown but not the irreversible one would be the wrong way round
        /// (§SEC14.7 posture A′ rule 4), and §SEC18.6 rule 2 makes the veto overridable by
        /// nobody, administrators included. This member composes nothing.</para>
        /// </summary>
        ValueTask<Association> HardRemoveAssociationByIdAsync(
            Guid associationId,
            CancellationToken cancellationToken = default);
    }
}
