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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// The workflow's own transition on Berean's assignment: returning one to pending when the
    /// content it judged has changed under it (§8.8 rule 1) or the round's outcome has been
    /// overridden (§8.6 HR-4). It is kept apart from the public modify verb because the two are
    /// different acts by different actors — see
    /// <see cref="IAIReviewerAssignmentWorkflowService"/> — and because the system identity this
    /// runs under holds no roles, so the modify gate would refuse it.
    ///
    /// <para>Like every transition it loads the row FIRST and authorizes against what is STORED;
    /// the request carries only the id.</para>
    /// </summary>
    internal partial class AIReviewerAssignmentService : IAIReviewerAssignmentWorkflowService
    {
        public ValueTask<AIReviewerAssignment> ReturnStaleAIReviewerAssignmentToPendingAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The transition owns both flags and drives them to a fixed pair, so the request
                // carries nothing but the id — the entity exists to anchor the security context
                // and the causation chain, exactly as the read path's does.
                var returnToPendingRequest = new AIReviewerAssignment { Id = aiReviewerAssignmentId };

                EventEnvelope<AIReviewerAssignment> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: returnToPendingRequest);

                return await DoReturnStaleAIReviewerAssignmentToPendingAsync(
                    aiReviewerAssignmentId: aiReviewerAssignmentId,
                    inboundEnvelope: systemEnvelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<AIReviewerAssignment> DoReturnStaleAIReviewerAssignmentToPendingAsync(
            Guid aiReviewerAssignmentId,
            EventEnvelope<AIReviewerAssignment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // Deliberately NOT ValidateUserIsAllowedToManageAIReviewerAssignments, which the
            // public write paths use: the system identity holds no roles, so asking for the
            // review tier here would refuse the only caller this verb has. The contribution half
            // of that gate still runs, and the system-identity check below is what actually
            // stands in for authorization.
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnReturnStaleAIReviewerAssignmentToPending(aiReviewerAssignmentId);
            ValidateReturnToPendingIsTheWorkflowsOwnAct(inboundEnvelope.SecurityContext);

            AIReviewerAssignment maybeAIReviewerAssignment =
                await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId, cancellationToken);

            ValidateStorageAIReviewerAssignment(maybeAIReviewerAssignment, aiReviewerAssignmentId);

            // WITHDRAWN — a moderator took Berean off the round between the gather that named
            // this id and this write. Returned unchanged rather than refused, which looks like it
            // contradicts the public modify's IsDeleted guard and does not: that guard exists
            // because a PERSON asked for something and must learn it did not happen, so
            // answering quietly would tell them their write landed. The caller here is the
            // workflow, and it is not being told its write landed but that there is nothing left
            // to put back — the same answer the remove path gives an already-removed row.
            if (maybeAIReviewerAssignment.IsDeleted)
                return maybeAIReviewerAssignment;

            // ALREADY PENDING, and returned unchanged rather than refused. This is the
            // ApprovalReviewRequest posture and deliberately not the ApprovalReview one: a second
            // dismissal is refused because Dismissed is a state that must not be re-entered,
            // while pending is the TARGET state — and after an administrator resets a round whose
            // entity was also just edited, two legitimate paths reach this row inside one act.
            // Refusing would turn an ordering coincidence into an error, and publishing a second
            // fact for a row that did not move would tell subscribers something happened when
            // nothing did.
            if (maybeAIReviewerAssignment.IsAIReviewCompleted is false
                && maybeAIReviewerAssignment.IsAIReviewCommentsPresent is false)
            {
                return maybeAIReviewerAssignment;
            }

            // The two move as a PAIR, and the pairing invariant is honoured by construction
            // rather than re-asserted as a rule: IsAIReviewCommentsPresent records something
            // Berean left behind and cannot stand on a row that says the pass never finished
            // (see ValidateOnModifyAIReviewerAssignmentAsync's own note), and the fixed target
            // here is (false, false), which that rule cannot fire on. There is nothing to read
            // off a caller's copy — the same reasoning the sibling dismissal gives for its fixed
            // status target.
            maybeAIReviewerAssignment.IsAIReviewCompleted = false;
            maybeAIReviewerAssignment.IsAIReviewCommentsPresent = false;

            AIReviewerAssignment auditedAIReviewerAssignment =
                await this.securityAuditBroker.ApplyModifyAuditValuesAsync(
                    entity: maybeAIReviewerAssignment,
                    securityContext: inboundEnvelope.SecurityContext);

            AIReviewerAssignment updatedAIReviewerAssignment =
                await this.storageBroker.UpdateAIReviewerAssignmentAsync(
                    aiReviewerAssignment: auditedAIReviewerAssignment,
                    cancellationToken: cancellationToken);

            // NO ProcessedEvents bookkeeping on this path, matching both siblings. The dual
            // record exists so a do-work shared between a public verb and an event handler cannot
            // process one delivery twice — the verb pre-records the id against the handler's
            // receiver name and the handler then skips it. This verb has no event address and no
            // handler, so both rows would be written with no reader.
            EventEnvelope<AIReviewerAssignment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedAIReviewerAssignment);

            // The ordinary Modified fact rather than a reset-specific one: the row changed either
            // way, AIReviewerAssignmentEventOperation has no reset member and §8.6.2 introduces
            // no separate address. What distinguishes this from a moderator's re-request is
            // recorded ON the row — UpdatedBy names the system identity. Minting an address
            // instead would also owe §10.17(a) a subscriber for it, and nothing is listening.
            await this.eventBroker.PublishAIReviewerAssignmentAsync(
                envelope: outboundEnvelope,
                operation: AIReviewerAssignmentEventOperation.Modified);

            return updatedAIReviewerAssignment;
        }
    }
}
