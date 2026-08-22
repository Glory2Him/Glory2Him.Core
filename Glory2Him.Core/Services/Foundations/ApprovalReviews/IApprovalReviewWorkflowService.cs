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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    /// <summary>
    /// The writes the approval workflow makes on its own behalf, which no human is permitted to
    /// make (design §9.7.4, #196 decision 9).
    /// </summary>
    /// <remarks>
    /// <para><b>Deliberately separate from <see cref="IApprovalReviewService"/>, and deliberately
    /// internal.</b> That interface is public and the portal binds its controllers straight to
    /// it; a "dismiss as the workflow" member on it would hand every endpoint a way to act with
    /// the system's authority. This one is reachable only from inside Core.</para>
    ///
    /// <para><b>The caller never supplies the identity.</b> It asks for the act, and the service
    /// mints the system context itself. That is the whole security property: the system-identity
    /// flag is honoured only where this service minted it, because an envelope arriving over a
    /// public event address carries a deserialized, unverified context (§14.6 rule 4) and anyone
    /// able to assert the flag there could dismiss any review in the system by declaring
    /// themselves the workflow.</para>
    /// </remarks>
    internal interface IApprovalReviewWorkflowService
    {
        /// <summary>
        /// Dismisses a review the content it judged has since changed, under the system identity.
        /// </summary>
        /// <remarks>
        /// <para>Dismissing stale reviews after the OWNER's edit is a write the workflow must
        /// make and no human is permitted to: the owner holds no publisher tier, and the
        /// reviewers whose reviews are being withdrawn are the last parties who should withdraw
        /// them. Automatic dismissal is not a user action, any more than automatic approval
        /// is.</para>
        ///
        /// <para>The deciding human is still recorded — the system context keeps their
        /// <c>SubjectId</c>, because the audit answer to "who caused this" is a person. What it
        /// drops is their roles: the flag stands in for the publisher tier by itself, and
        /// carrying both would leave a context that looks authorised two different ways.</para>
        /// </remarks>
        ValueTask<ApprovalReview> DismissStaleApprovalReviewAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default);
    }
}
