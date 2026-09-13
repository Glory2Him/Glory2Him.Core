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
        // THE ONE THING THAT DIFFERS BETWEEN THE TWO HANDLERS, and the only thing that may.
        //
        // Stated as literals because there is nowhere to read them from: EventBroker composes the
        // signed name as entityName + operation on the PUBLISH side alone and exposes that
        // composition to nobody, so every receiver in this solution states its own. #286 is the
        // sweep that would introduce a canonical map, and a bespoke derivation at two more sites
        // would pre-empt a ruling scoped to all of them.
        //
        // Composed from the ENTITY and the OPERATION, never from the tense of the address:
        // Approval-Added and Approval-Modified are the first of the Approval entity's own FACT
        // addresses to carry a subscription from this service, and that changes nothing about the
        // name.
        private const string ApprovalAddedEventName = "ApprovalAdded";
        private const string ApprovalModifiedEventName = "ApprovalModified";

        public ValueTask<EventEnvelope<Approval>?> OnApprovalAddedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Approval>?>(async () =>
            {
                await ValidateApprovalFactEnvelopeAsync(envelope, ApprovalAddedEventName);

                await AssignAIReviewerAutomaticallyAsync(
                    approval: envelope.Content,
                    cancellationToken: cancellationToken);

                return null;
            });

        public ValueTask<EventEnvelope<Approval>?> OnApprovalModifiedAsync(
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken = default) =>
            TryCatch<EventEnvelope<Approval>?>(async () =>
            {
                await ValidateApprovalFactEnvelopeAsync(envelope, ApprovalModifiedEventName);

                await AssignAIReviewerAutomaticallyAsync(
                    approval: envelope.Content,
                    cancellationToken: cancellationToken);

                return null;
            });

        // The shared body both subscriptions delegate to. A fact is a notification, so nothing is
        // replied with: returning the inbound envelope would put this service's name on a fact
        // another service published.
        private async ValueTask AssignAIReviewerAutomaticallyAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            // GATE 2. A soft-deleted round gets no reviewer. Read off the SIGNED content rather
            // than re-read from storage: this is the row the foundation itself published, and it
            // is inside the HMAC the gate above has already verified.
            if (approval.IsDeleted)
            {
                return;
            }

            await this.aiReviewerAssignmentWorkflowService
                .AddAutomaticAIReviewerAssignmentAsync(
                    approvalId: approval.Id,
                    cancellationToken: cancellationToken);
        }
    }
}
