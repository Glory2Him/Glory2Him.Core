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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    internal partial class AIReviewerAssignmentService
    {
        public ValueTask<AIReviewerAssignment?> RetrieveAIReviewerAssignmentByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatchNullable(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveAIReviewerAssignmentByApprovalId(approvalId);

                // the envelope exists to capture the ambient security context the read gate
                // runs against — the request payload is empty, exactly as every other unkeyed
                // or round-keyed foundation read builds it
                EventEnvelope<AIReviewerAssignment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new AIReviewerAssignment());

                SecurityContext? securityContext = envelope.SecurityContext;

                // Denied the SAME WAY a missing assignment is answered: null, never an exception.
                // This read already has a legitimate "nothing here" outcome (Berean was never
                // assigned to this round), so folding "you may not see it" into that same answer
                // is the not-found-never-unauthorized posture (§14.6 rule 12) rather than a
                // deviation from it — there is simply no separate not-found shape to distinguish
                // it from, the way a keyed-by-id read has one.
                if (securityContext is null
                    || securityContext.IsAuthenticated is false
                    || HasReviewRole(securityContext) is false)
                {
                    await this.loggingBroker.LogWarningAsync(
                        message: $"AI reviewer assignment read denied. The caller is not in a " +
                            $"review role; reported for approval {approvalId} as no assignment.");

                    return null;
                }

                // THE ROUND'S ONE ROW, ASKED FOR AS ONE ROW — not a list narrowed down after the
                // fact. The storage broker filters to the live row directly (see
                // IStorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync for why that is safe
                // here and is not the layering violation it would be on a read with an actual
                // visibility posture to re-apply).
                return await this.storageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);
            });

        // An unresolved round would key the read on Guid.Empty and answer with "no assignment",
        // which a caller reads as "Berean was never assigned" rather than as the bug it is.
        private static void ValidateOnRetrieveAIReviewerAssignmentByApprovalId(Guid approvalId) =>
            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(AIReviewerAssignment.ApprovalId)));
    }
}
