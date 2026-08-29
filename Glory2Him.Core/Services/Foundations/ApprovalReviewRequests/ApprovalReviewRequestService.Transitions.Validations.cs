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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    internal partial class ApprovalReviewRequestService
    {
        private static void ValidateOnRetireApprovalReviewRequest(Guid approvalReviewRequestId) =>
            Validate(
                message: "Approval review request is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalReviewRequestId),
                    Parameter: nameof(ApprovalReviewRequest.Id)));

        // Retirement belongs to the approval workflow alone (§7.9 rule 6). A person who wants an
        // invitation gone withdraws it through the public verb, which records THEM as the
        // remover; this one records the system, and it means something different — that the
        // invited person answered.
        //
        // Unreachable in practice, and deliberately kept — but narrower than it looks. The one
        // public seam, RetireAnsweredApprovalReviewRequestAsync, calls CreateSystemAsync itself
        // before delegating, so anything entering THERE mints a passing context by construction
        // and can never fail this. What it actually guards is a future second caller of the
        // private DoRetireAnsweredApprovalReviewRequestAsync that supplies its own envelope.
        private static void ValidateRetirementIsTheWorkflowsOwnAct(SecurityContext securityContext)
        {
            if (securityContext.IsSystemIdentity is false)
            {
                throw new UnauthorizedApprovalReviewRequestException(
                    message: "Retiring an answered approval review request is the approval "
                        + "workflow's own act; no user may perform it.");
            }
        }
    }
}
