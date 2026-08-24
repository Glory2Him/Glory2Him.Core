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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    /// <summary>
    /// The approval workflow's own operations on the <c>Approval</c> record, performed under the
    /// system identity because they are not any human's act (#196 decision 9, #287).
    /// </summary>
    /// <remarks>
    /// <para><b>Why a separate surface rather than the public one.</b> Every member here has a
    /// twin on <see cref="IApprovalService"/> that gates on the CALLER — owner or review role for
    /// the read and the amend, and the publisher tier plus §8.6's regardless rules for a decision.
    /// Those gates are right for a person and wrong for the workflow, which holds no roles at all
    /// and is not acting on anyone's behalf.</para>
    ///
    /// <para><b>The bug this exists to fix.</b> §8.5's re-test begins by reading the approval, and
    /// the public read reports not-found to a caller who is neither the owner nor a review-role
    /// holder. Commenting deliberately carries no tier — <i>"anyone who may contribute may speak
    /// on an approval they can see"</i> — so a contributor acting on their OWN comment triggers a
    /// workflow reaction that throws. The comment service discards the publish result, so it
    /// fails silently and four of the workflow's subscriptions are inert for that actor class.</para>
    ///
    /// <para><b>The caller never supplies the identity.</b> It asks for the act; this service
    /// mints the system context itself through <c>CreateSystemAsync</c>. That is what makes the
    /// flag unforgeable by construction rather than by validation — it has exactly one writer in
    /// the solution, and no token, claim, role or header can produce it (§16.7.1).</para>
    ///
    /// <para><b>All four members, not only the two that were broken.</b> The orchestration reaches
    /// <c>Approval</c> through this interface alone, so it cannot call a caller-gated twin by
    /// accident — the same compile-time narrowing #295 gave the review seam. Add and the entity
    /// lookup are gated only by the contribution check and would work either way; they are here
    /// so that the orchestration's whole Approval surface is one type with one identity story.</para>
    /// </remarks>
    internal interface IApprovalWorkflowService
    {
        /// <summary>
        /// Opens a round for an entity that has been submitted.
        /// </summary>
        /// <remarks>
        /// The system context keeps the submitter's <c>SubjectId</c>, so <c>CreatedBy</c> still
        /// names the person whose submission opened the round — the audit answer to "who caused
        /// this" is a person, and only the roles are dropped.
        /// </remarks>
        ValueTask<Approval> AddApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a round UNFILTERED by the caller's relationship to it.
        /// </summary>
        /// <remarks>
        /// What a round IS is a fact about storage, not about who is asking. The public read's
        /// owner-or-review-role filter is a visibility posture for people; applying it to the
        /// workflow's own re-test makes the evaluation depend on which actor happened to trigger
        /// it, which is the defect this interface exists for.
        /// </remarks>
        ValueTask<Approval> RetrieveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the workflow's own decision onto the round.
        /// </summary>
        /// <remarks>
        /// Skips the three caller tiers together — the row-local owner-or-review-role test, the
        /// entity-narrowed amend gate, and §8.6.1's decision function. They ask whether a PERSON
        /// may decide; an automatic approval fired by the last reviewer's own review has no
        /// person deciding it, and the reviewer whose review completed the round is the one party
        /// §8.6 regardless-rule 1 forbids from applying the outcome.
        /// </remarks>
        ValueTask<Approval> ModifyApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the round belonging to an entity, or null when none exists.
        /// </summary>
        /// <remarks>
        /// Gated only by the contribution check on the public side too, so this member carries no
        /// behavioural difference — it is here so the orchestration holds one Approval surface
        /// rather than two.
        /// </remarks>
        ValueTask<ApprovalEntityMatch?> FindApprovalByEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);
    }
}
