// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial class ApprovalCommentService
    {
        public ValueTask<IReadOnlyList<ApprovalComment>> RetrieveApprovalCommentsByApprovalIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            TryCatchList(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalCommentsByApprovalId(approvalId);

                // the envelope exists to capture the ambient security context the visibility
                // filter runs against - the request payload is empty, exactly as the unkeyed
                // collection read builds it
                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new ApprovalComment());

                // THE ROUND'S SLICE, ASKED FOR AS A SLICE, with the token. The unkeyed read hands
                // back a live queryable, so the name resolver narrowing it to one approval had
                // only a synchronous terminal operator to execute it with.
                List<ApprovalComment> approvalComments =
                    await this.storageBroker.SelectApprovalCommentsByApprovalIdAsync(
                        approvalId: approvalId,
                        cancellationToken: cancellationToken);

                // THE SAME FILTER, not a second copy of it. §14.7 posture D decides whose words a
                // reader may see, and it is re-applied here over the materialised rows rather
                // than restated - naming somebody whose comment is hidden from the reader would
                // say more than the thread does.
                IQueryable<ApprovalComment> visibleApprovalComments =
                    await ApplyCollectionReadVisibilityFilterAsync(
                        approvalComments: approvalComments.AsQueryable(),
                        securityContext: envelope.SecurityContext);

                return visibleApprovalComments.ToList();
            });
    }
}
