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

namespace Glory2Him.Core.Models.Events.Foundations
{
    /// <summary>
    /// The operations an <c>ApprovalReviewRequest</c> event can represent — requests (present
    /// tense: <see cref="Adding"/>, <see cref="RemovingById"/>, <see cref="HardRemovingById"/>,
    /// <see cref="RetrievingById"/>) answered by responder handlers, and facts (past tense:
    /// <see cref="Added"/>, <see cref="Removed"/>, <see cref="HardRemoved"/>) published by the
    /// service after the work is done. Every request operation maps to its own event address
    /// (for example <c>ApprovalReviewRequest-Adding</c>) and composes the stored event name (for
    /// example <c>"ApprovalReviewRequestAdding"</c>). <see cref="HardRemoved"/> shares the
    /// <see cref="Removed"/> event address and is distinguished purely by its event name.
    ///
    /// <para><b>There is deliberately no Modifying operation.</b> An invitation has nothing
    /// amendable. Its two load-bearing fields — <c>ApprovalId</c> and <c>RequestedUserId</c> —
    /// are the halves of <c>UX_ApprovalReviewRequests_ApprovalId_RequestedUserId</c> and are
    /// fixed at creation, exactly as the review index's halves are; moving either would walk the
    /// row past the uniqueness rule that makes §7.9 rule 1 mean anything, and re-pointing a
    /// request at a different person is a new invitation rather than an edit of the old one. The
    /// only remaining field, <c>RequestedUserDisplayName</c>, is cosmetic. So the lifecycle is
    /// request and withdraw: a mistaken invitation is corrected by withdrawing it (§7.9 rule 5)
    /// and issuing another, which leaves both acts in the audit trail instead of overwriting the
    /// first.</para>
    /// </summary>
    public enum ApprovalReviewRequestEventOperation
    {
        Adding,
        RemovingById,
        HardRemovingById,
        RetrievingById,

        Added,
        Removed,
        HardRemoved
    }
}
