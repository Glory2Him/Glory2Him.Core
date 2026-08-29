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

using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.EventEnvelopes
{
    /// <summary>
    /// The single place event envelopes are shaped, so metadata, security context, and
    /// causation chaining never diverge between services or between the event and non-event
    /// paths.
    /// </summary>
    internal interface IEventEnvelopeBroker
    {
        /// <summary>
        /// Creates a root envelope for a new operation: fresh event and correlation
        /// identifiers, and the current caller captured as the <c>SecurityContext</c>.
        /// Used by the non-event path to convert an incoming object into the envelope
        /// currency before calling the shared do-work methods.
        /// </summary>
        ValueTask<EventEnvelope<T>> CreateAsync<T>(T content);

        /// <summary>
        /// Creates the next envelope in a causation chain: fresh event identifier,
        /// <c>CausationId</c> pointing at the source envelope's event, and the source's
        /// security and request context carried forward unchanged. Used for outbound events
        /// emitted by do-work methods and for replies returned by responder handlers.
        /// </summary>
        /// <summary>
        /// Mints an envelope carrying the WORKFLOW's identity rather than a plain caller's, for a
        /// write no human is permitted to make directly (design §16.7.1) — the approval workflow
        /// syncing a decision onto its entity.
        ///
        /// <para>The actor recorded is <c>SystemIdentity.UserId</c>, because no human performed
        /// this: an approval opens because content was submitted, a round re-approves because its
        /// conditions came to be met, an invitation retires because the person answered it.
        /// Stamping whichever human's request happened to be on the stack would name somebody who
        /// did not act — and on <c>Approval</c>, whose rows the workflow owns outright, would
        /// forge an owner. The triggering person is kept on <c>DelegatedBySubjectId</c>, so the
        /// causal trail survives without the audit columns lying. Roles are dropped, because the
        /// system identity stands in for the publisher tier and reading roles off it would
        /// suggest an authority it is not exercising.</para>
        ///
        /// <para>For the manual approve or reject — an act a person really did perform, but is
        /// not permitted to write directly — use <see cref="CreateElevatedAsync"/> instead.</para>
        ///
        /// <para>The claim is only worth anything because it is signed: envelopes leave this
        /// system signed with a key only this system holds, and the security context is inside
        /// the signed payload (§14.6 rule 4).</para>
        /// </summary>
        ValueTask<EventEnvelope<T>> CreateSystemAsync<T>(T content);

        /// <summary>
        /// Mints an envelope for a write the workflow performs while CARRYING OUT a person's
        /// decision — the manual approve or reject, with or without bypass. The ambient caller is
        /// retained, so the entity's <c>UpdatedBy</c> records the administrator whose decision
        /// this is; roles are dropped exactly as for <see cref="CreateSystemAsync"/>, because the
        /// system identity stands in for the publisher tier and reading roles off it would
        /// suggest an authority it is not exercising.
        ///
        /// <para><b>How this differs from <see cref="CreateSystemAsync"/>.</b> That one is for
        /// acts nobody asked for, and records <c>SystemIdentity.UserId</c>. This one is for an act
        /// a person asked for but is not permitted to write directly, and records the person. The
        /// caller chooses which ACT it is performing; it never supplies an identity, so it can
        /// only ever elect to be recorded as itself (§16.7.1).</para>
        /// </summary>
        ValueTask<EventEnvelope<T>> CreateElevatedAsync<T>(T content);

        ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content);
    }
}
