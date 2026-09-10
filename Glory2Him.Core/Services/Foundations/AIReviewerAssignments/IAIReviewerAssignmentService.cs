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
    public partial interface IAIReviewerAssignmentService
    {
        ValueTask<AIReviewerAssignment> AddAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> RetrieveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The one LIVE assignment on an approval round, or <c>null</c> when Berean has never
        /// been assigned to it.
        ///
        /// <para>Singular, unlike <c>ApprovalReviewRequestService</c>'s round-keyed read: that
        /// entity supports many different requested users per approval, while at most one
        /// <see cref="AIReviewerAssignment"/> row is ever live for a round, so there is no list
        /// to hand back — only the row, or the absence of one.</para>
        /// </summary>
        ValueTask<AIReviewerAssignment?> RetrieveAIReviewerAssignmentByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> ModifyAIReviewerAssignmentAsync(
            AIReviewerAssignment aiReviewerAssignment,
            CancellationToken cancellationToken = default);

        ValueTask<AIReviewerAssignment> RemoveAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        // There is deliberately no HardRemove member — see AIReviewerAssignmentEventOperation
        // for the same note on the event side.
    }
}
