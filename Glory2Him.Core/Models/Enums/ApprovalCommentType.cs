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

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// What an <c>ApprovalComment</c> IS — a remark, or an ask (design §7.8).
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is not <c>IsResolved</c>.</b> §7.8 already draws the line the surface
    /// needs — an observation asks for nothing and is born settled, an ask holds the approval shut
    /// until it is settled — but it draws it in a field that MOVES. The moment a question is
    /// settled its <c>IsResolved</c> is <c>true</c>, which is exactly what an informational comment
    /// looks like at birth, and the two become indistinguishable. A thread cannot then say which
    /// rows were questions, and a resolve control cannot be offered on questions alone.</para>
    ///
    /// <para><b>The type says what the row is; the flag says where it stands.</b> They are related
    /// at BIRTH only — a <see cref="Question"/> is created outstanding and a <see cref="Comment"/>
    /// settled — and never afterwards. Nothing recomputes one from the other. §7.8 rule 1 still
    /// stands as a SHAPE rule — neither write path pins <c>IsResolved</c> — but a caller who states
    /// both is no longer taken at their word: the PAIRING is refused wherever it can be reached, at
    /// birth and on amend alike.</para>
    ///
    /// <para>APPEND-ONLY, and the members are persisted BY NAME — the storage broker converts this
    /// property with <c>HasConversion&lt;string&gt;()</c>, so a stored row reads "Question" rather
    /// than 1. The numbers are still a contract: the host registers no <c>JsonStringEnumConverter</c>,
    /// so they are what crosses the wire to the React client, and the TypeScript mirror is numbered
    /// to match.</para>
    /// </remarks>
    public enum ApprovalCommentType
    {
        /// <summary>
        /// A remark. An observation, or a reviewer recording their rationale so others can see the
        /// thinking behind a verdict. Nothing waits on it, so it is created settled and never
        /// blocks (§7.8).
        ///
        /// <para>ZERO on purpose: it is what every row written before the column existed was, and
        /// what a caller who says nothing means.</para>
        /// </summary>
        Comment = 0,

        /// <summary>
        /// An ask — a question, or a change request. It is created OUTSTANDING and holds the
        /// approval shut under <c>RequireReviewCommentResolutionBeforeApprovals</c> until somebody
        /// entitled to settles it.
        /// </summary>
        Question = 1
    }
}
