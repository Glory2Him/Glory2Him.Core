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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    public partial interface IApprovalReviewRequestService
    {
        ValueTask<ApprovalReviewRequest> AddApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ApprovalReviewRequest>> RetrieveAllApprovalReviewRequestsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> RetrieveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> RemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ApprovalReviewRequest> HardRemoveApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId,
            CancellationToken cancellationToken = default);

        // There is deliberately no Modify member, and it is not an omission. An invitation has
        // nothing amendable: ApprovalId and RequestedUserId are the halves of
        // UX_ApprovalReviewRequests_ApprovalId_RequestedUserId and are fixed at creation, and
        // the only other field is a cosmetic display name. A mistaken invitation is withdrawn
        // (§7.9 rule 5) and re-issued, which leaves both acts in the audit trail rather than
        // overwriting the first. See ApprovalReviewRequestEventOperation for the same note on
        // the event side.
    }
}
