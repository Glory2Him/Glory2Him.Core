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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// §8.6.2.1's automatic assignment — Berean goes onto a round that has entered review,
    /// wherever the resolved policy asks for it, with nobody having pressed anything.
    ///
    /// <para><b>Nobody's act, so nobody's identity.</b> The write goes through the WORKFLOW seam,
    /// which mints the system identity in process, rather than through the caller-facing
    /// foundation whose gate asks for a review tier the system identity does not hold — and which
    /// would in any case record the wrong author, since the identity on the inbound envelope
    /// belongs to whoever moved the round.</para>
    /// </summary>
    internal partial class AIReviewerOrchestrationService
    {
        public ValueTask<EventEnvelope<Approval>?> OnApprovalAddedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Approval>?>(async () =>
            {
                await AssignAIReviewerAutomaticallyAsync(
                    approvalId: envelope.Content.Id,
                    cancellationToken: cancellationToken);

                return null;
            });

        public ValueTask<EventEnvelope<Approval>?> OnApprovalModifiedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Approval>?>(async () =>
            {
                await AssignAIReviewerAutomaticallyAsync(
                    approvalId: envelope.Content.Id,
                    cancellationToken: cancellationToken);

                return null;
            });

        // The shared body both subscriptions delegate to. A fact is a notification, so nothing is
        // replied with: returning the inbound envelope would put this service's name on a fact
        // another service published.
        private async ValueTask AssignAIReviewerAutomaticallyAsync(
            Guid approvalId,
            CancellationToken cancellationToken)
        {
            await this.aiReviewerAssignmentWorkflowService
                .AddAutomaticAIReviewerAssignmentAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);
        }
    }
}
