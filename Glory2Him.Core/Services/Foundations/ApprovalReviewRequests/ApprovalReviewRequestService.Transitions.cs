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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// The workflow's own transition on an invitation: retiring one that has been answered
    /// (§7.9 rule 6). It is kept apart from the public withdraw verb because the two are
    /// different acts by different actors — see
    /// <see cref="IApprovalReviewRequestWorkflowService"/> — and because the system identity the
    /// retirement runs under holds no roles, so the withdraw gate would refuse it.
    ///
    /// <para>Like every transition it loads the row FIRST and authorizes against what is STORED;
    /// the request carries only the id.</para>
    /// </summary>
    internal partial class ApprovalReviewRequestService : IApprovalReviewRequestWorkflowService
    {
        // What DeletionReason records on a retirement, so a reader can tell it from a withdrawal
        // at a glance. The operation owns the value outright — there is nothing to read off a
        // caller's copy, exactly as dismissal owns its fixed target status.
        private const string AnsweredRetirementReason =
            "Retired: the invited reviewer recorded their review.";

        public ValueTask<ApprovalReviewRequest> RetireAnsweredApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retirement owns only IsDeleted and drives it to a fixed value, so the request
                // carries nothing but the id — the entity exists to anchor the security context
                // and the causation chain, exactly as the read path's does.
                var retireRequest = new ApprovalReviewRequest { Id = approvalReviewRequestId };

                EventEnvelope<ApprovalReviewRequest> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: retireRequest);

                return await DoRetireAnsweredApprovalReviewRequestAsync(
                    approvalReviewRequestId: approvalReviewRequestId,
                    inboundEnvelope: systemEnvelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalReviewRequest> DoRetireAnsweredApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // Deliberately NOT the review-tier gate the withdraw path uses: the system identity
            // holds no roles, so asking for one here would refuse the only caller this verb has.
            // The contribution gate still runs, and the system-identity check below is what
            // actually stands in for authorization.
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRetireApprovalReviewRequest(approvalReviewRequestId);
            ValidateRetirementIsTheWorkflowsOwnAct(inboundEnvelope.SecurityContext);

            ApprovalReviewRequest maybeApprovalReviewRequest =
                await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                    approvalReviewRequestId, cancellationToken);

            ValidateStorageApprovalReviewRequest(maybeApprovalReviewRequest, approvalReviewRequestId);

            // Already gone — withdrawn by a moderator before the invited person got to it, or
            // retired by a redelivered fact. Returned unchanged and publishing nothing, matching
            // the withdraw path: a second removal fact for a row that is already removed would
            // tell subscribers something happened when nothing did.
            if (maybeApprovalReviewRequest.IsDeleted)
                return maybeApprovalReviewRequest;

            ApprovalReviewRequest auditedApprovalReviewRequest =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalReviewRequest,
                    securityContext: inboundEnvelope.SecurityContext,
                    deletionReason: AnsweredRetirementReason);

            ApprovalReviewRequest retiredApprovalReviewRequest =
                await this.storageBroker.UpdateApprovalReviewRequestAsync(
                    approvalReviewRequest: auditedApprovalReviewRequest,
                    cancellationToken: cancellationToken);

            // NO ProcessedEvents bookkeeping on this path, matching the sibling's dismissal. The
            // dual record exists so a do-work shared between a public verb and an event handler
            // cannot process one delivery twice — the verb pre-records the id against the
            // handler's receiver name and the handler then skips it. This verb has no event
            // address and no handler, so both rows would be written with no reader.
            EventEnvelope<ApprovalReviewRequest> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: retiredApprovalReviewRequest);

            // The ordinary removal fact rather than a retirement-specific one: the row is gone
            // either way, and §7.9 introduces no separate address. What distinguishes the two is
            // recorded ON the row — DeletedBy names the system identity and DeletionReason says
            // it was answered.
            await this.eventBroker.PublishApprovalReviewRequestAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewRequestEventOperation.Removed);

            return retiredApprovalReviewRequest;
        }
    }
}
