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
    /// The operations an <c>AIReviewerAssignment</c> event can represent — requests (present
    /// tense: <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="RetrievingById"/>) answered by responder handlers, and facts (past tense:
    /// <see cref="Added"/>, <see cref="Modified"/>, <see cref="Removed"/>) published by the
    /// service after the work is done. Every request operation maps to its own event address
    /// (for example <c>AIReviewerAssignment-Adding</c>) and composes the stored event name (for
    /// example <c>"AIReviewerAssignmentAdding"</c>).
    ///
    /// <para><b>There is deliberately no HardRemove operation.</b> The template this service
    /// mirrors (<c>ApprovalReviewRequest</c>) has one, but nothing in this pass needs it — a
    /// soft-deleted assignment already frees the round's one-live-assignment slot (see
    /// <c>AIReviewerAssignment.IsDeleted</c>), and adding the hard-remove capability without a
    /// caller that needs it would be untested surface for its own sake.</para>
    /// </summary>
    public enum AIReviewerAssignmentEventOperation
    {
        Adding,
        Modifying,
        RemovingById,
        RetrievingById,

        Added,
        Modified,
        Removed
    }
}
