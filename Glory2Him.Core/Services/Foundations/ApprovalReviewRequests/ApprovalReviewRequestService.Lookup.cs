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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    internal partial class ApprovalReviewRequestService
    {
        public ValueTask<IReadOnlyList<ApprovalReviewRequest>> RetrieveApprovalReviewRequestsByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the visibility
                // filter runs against — the request payload is empty, exactly as the unkeyed
                // collection read builds it
                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalReviewRequest());

                // THE ROUND'S SLICE, ASKED FOR AS A SLICE. The unkeyed read returns a live
                // queryable, so every caller wanting one approval's invitations composed a Where
                // onto it and executed it with a synchronous terminal operator — a blocking SQL
                // round trip on the request thread, and a cancellation token that reached every
                // call in the chain except the one that touches the database. The narrowing and
                // the await both belong to the layer that owns EF, so both moved there.
                List<ApprovalReviewRequest> approvalReviewRequests =
                    await this.storageBroker.SelectApprovalReviewRequestsByApprovalIdAsync(
                        approvalId: approvalId,
                        cancellationToken: cancellationToken);

                // THE SAME FILTER, not a second copy of it. The broker read is deliberately
                // unfiltered beyond the id, and the §14.7 posture is re-applied here over the
                // materialised rows — the predicate is now evaluated in memory rather than in
                // SQL, which is the only difference, and it is one the caller cannot observe.
                // Writing an in-memory twin of the filter would have given one rule two homes,
                // and a visibility rule is the last rule that should be allowed to drift.
                IQueryable<ApprovalReviewRequest> visibleApprovalReviewRequests =
                    await ApplyCollectionReadVisibilityFilterAsync(
                        approvalReviewRequests: approvalReviewRequests.AsQueryable(),
                        securityContext: envelope.SecurityContext);

                return visibleApprovalReviewRequests.ToList();
            });
    }
}
