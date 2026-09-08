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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// The approval workflow's own write seam on <c>AIReviewerAssignment</c>, separate from the
    /// public <see cref="IAIReviewerAssignmentService"/> the exposers bind to. Same
    /// implementation, a narrower door.
    ///
    /// <para><b>Why it exists.</b> Two rules make a round's verdicts stop describing its content
    /// — §8.8 rule 1, when an edit lands under <c>RequireReapprovalOnChange</c>, and §8.6 HR-4,
    /// when an administrator overrides a decided round — and the human reviews are dismissed at
    /// both. Berean's assignment is keyed on the APPROVAL rather than on those reviews, so it
    /// survives both untouched and goes on reporting a finished pass, with comments, over text it
    /// never saw. The reset that takes that back runs under the SYSTEM identity, which
    /// <c>IEventEnvelopeBroker.CreateSystemAsync</c> deliberately mints with no roles — the system
    /// flag stands in for the tier by itself — so the public modify verb cannot serve it: its gate
    /// asks for a review-tier role and the system identity holds none. Without this seam the
    /// orchestration's only options would be to forge a role-bearing context, or to call the
    /// public verb as the EDITOR, who in the ordinary case is the author revising their own
    /// submission and holds no review role either (HR-1).</para>
    ///
    /// <para><b>A re-request and a reset are different acts</b>, which is why this is its own verb
    /// rather than a bypass bolted onto <c>ModifyAIReviewerAssignmentAsync</c>. They write the
    /// same two fields, but a moderator asking Berean to look again is a PERSON's decision and
    /// <c>UpdatedBy</c> must name them; this one is nobody's decision — it records that the
    /// content moved under a pass nobody re-requested — and <c>UpdatedBy</c> names the system.
    /// The same split the human half already has between a withdrawal and a retirement.</para>
    ///
    /// <para><c>internal</c> states the intent rather than enforcing it against every assembly —
    /// Core names <c>Glory2Him.WebApp</c> in <c>InternalsVisibleTo</c>. What it does enforce is
    /// the idiomatic route: a public controller cannot take an internal type through its
    /// constructor (CS0051), so reaching this from a portal would take a deliberate, conspicuous
    /// service-locator call rather than ordinary injection.</para>
    /// </summary>
    internal interface IAIReviewerAssignmentWorkflowService
    {
        /// <summary>
        /// Returns an assignment whose round has moved on back to PENDING — Berean stays on the
        /// round, and only the two flags recording what its pass reported go back. The caller
        /// does not hand the context in, and could not — it asks for the ACT and the service
        /// mints the identity, which is what makes the system flag unforgeable by construction
        /// rather than by validation.
        ///
        /// <para>An assignment that is already pending, or that has been withdrawn, is returned
        /// unchanged and publishes nothing, so a retried reset — or two paths reaching one row in
        /// a single act — cannot emit a second fact for a row that did not move.</para>
        /// </summary>
        ValueTask<AIReviewerAssignment> ReturnStaleAIReviewerAssignmentToPendingAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default);
    }
}
