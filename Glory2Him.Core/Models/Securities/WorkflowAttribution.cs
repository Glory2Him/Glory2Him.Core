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

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// Whose act a workflow write is, and therefore whose name its audit columns carry. The
    /// workflow performs both kinds through the same elevated seam, so the caller has to say
    /// which — the write itself looks identical either way.
    /// </summary>
    ///
    /// <remarks>
    /// This names an ACT, never an identity. A caller can elect to be recorded as itself or to
    /// record the system; it cannot name a third party, so the system flag stays unforgeable by
    /// construction rather than by validation (§16.7.1).
    /// </remarks>
    public enum WorkflowAttribution
    {
        /// <summary>
        /// Nobody asked. The approval opened because content was submitted, the round re-approved
        /// because its conditions came to be met, the entity was reinstated because a takedown was
        /// reversed. Recorded as <see cref="SystemIdentity.UserId"/>.
        /// </summary>
        System = 0,

        /// <summary>
        /// A person really did decide this — the manual approve or reject, with or without bypass
        /// — but is not permitted to write the row directly, so the workflow writes it for them.
        /// Recorded as the deciding caller, because the audit answer to "who approved this" is a
        /// human.
        /// </summary>
        DecidingCaller = 1
    }
}
