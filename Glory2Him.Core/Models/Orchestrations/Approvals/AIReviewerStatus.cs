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

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// Berean's status on a round (design §8.6.2) — one read answering both "should the picker
    /// even offer it" and "what has it done so far", for
    /// <c>GET api/Approvals/{entityType}/{entityId}/AIReviewer</c>.
    ///
    /// <para>Visible to the whole requesting tier (§7.9 rule 2) — Reviewers included, not
    /// narrowed to Publishers/Administrators the way <see cref="ApprovalVerdict"/> is: asking for
    /// Berean is coordination, exactly like asking a person, and a plain Reviewer may do
    /// either.</para>
    /// </summary>
    public class AIReviewerStatus
    {
        /// <summary>
        /// The resolved <c>ApprovalSetting.IsAIReviewerOffered</c> — whether Berean should be
        /// offered in the reviewer-request picker at all.
        /// </summary>
        public required bool IsOffered { get; init; }

        /// <summary>
        /// Whether a live <c>AIReviewerAssignment</c> exists for this round — the whole of what
        /// makes Berean's row appear in the round's own list, pending or completed.
        /// </summary>
        public required bool IsRequested { get; init; }

        /// <summary>
        /// <c>AIReviewerAssignment.IsAIReviewCompleted</c>, or <c>false</c> when nothing is
        /// assigned. Only ever set by the (not-yet-built) system process that runs Berean's
        /// analysis — stays <c>false</c> for as long as nothing does.
        /// </summary>
        public required bool IsAIReviewCompleted { get; init; }

        /// <summary>
        /// <c>AIReviewerAssignment.IsAIReviewCommentsPresent</c>, or <c>false</c> when nothing is
        /// assigned. Same provenance as <see cref="IsAIReviewCompleted"/>.
        /// </summary>
        public required bool IsAIReviewCommentsPresent { get; init; }
    }
}
