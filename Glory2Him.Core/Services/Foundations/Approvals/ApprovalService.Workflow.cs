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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    /// <summary>
    /// <see cref="IApprovalWorkflowService"/> — the approval workflow's own operations on the
    /// round, under an identity this service mints rather than one a caller supplies (#287).
    /// </summary>
    /// <remarks>
    /// Every member here differs from its public twin in exactly one line: the context comes from
    /// <c>CreateSystemAsync</c> instead of <c>CreateAsync</c>. Everything after that is the same
    /// shared do-work, so the workflow still runs the contribution gate, the not-found and
    /// soft-deleted guards, the audit stamp, the storage write and the fact publish. Only the
    /// tiers that ask "may this PERSON" are skipped, and only because there is no person.
    /// </remarks>
    internal partial class ApprovalService : IApprovalWorkflowService
    {
        ValueTask<Approval> IApprovalWorkflowService.AddApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalIsNotNull(approval);

                EventEnvelope<Approval> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: approval);

                return await DoAddApprovalAsync(
                    approval: approval,
                    inboundEnvelope: systemEnvelope,
                    cancellationToken: cancellationToken);
            });

        ValueTask<Approval> IApprovalWorkflowService.RetrieveApprovalByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Approval { Id = approvalId };

                EventEnvelope<Approval> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: retrieveRequest);

                return await DoRetrieveApprovalByIdAsync(
                    approvalId: approvalId,
                    inboundEnvelope: systemEnvelope,

                    // Admissible only because THIS service minted it, two lines up. The event
                    // path passes false — see OnRetrievingApprovalByIdAsync.
                    isSystemIdentity: true,
                    cancellationToken: cancellationToken);
            });

        ValueTask<Approval> IApprovalWorkflowService.ModifyApprovalAsync(
            Approval approval,
            CancellationToken cancellationToken) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalIsNotNull(approval);

                EventEnvelope<Approval> systemEnvelope =
                    await this.eventEnvelopeBroker.CreateSystemAsync(content: approval);

                return await DoModifyApprovalAsync(
                    isSystemIdentity: true,
                    approval: approval,
                    inboundEnvelope: systemEnvelope,
                    cancellationToken: cancellationToken);
            });

        ValueTask<ApprovalEntityMatch?> IApprovalWorkflowService.FindApprovalByEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken) =>
            FindApprovalByEntityAsync(
                entityType: entityType,
                entityId: entityId,
                cancellationToken: cancellationToken);
    }
}
