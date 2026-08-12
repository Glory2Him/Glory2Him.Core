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
    /// The operations a <c>Reaction</c> event can represent — requests (present tense:
    /// <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="HardRemovingById"/>, <see cref="RetrievingById"/>) answered by responder
    /// handlers, and facts (past tense: <see cref="Added"/>, <see cref="Modified"/>,
    /// <see cref="Removed"/>, <see cref="HardRemoved"/>) published by the service after the
    /// work is done. Every request operation maps to its own event address (for example
    /// <c>Reaction-Adding</c>) and composes the stored event name (for example
    /// <c>"ReactionAdding"</c>). <see cref="HardRemoved"/> shares the
    /// <see cref="Removed"/> event address and is distinguished purely by its event name
    /// (<c>"ReactionHardRemoved"</c>). Entity-specific operations may be appended here
    /// (with a matching event address in <c>EventBrokerIdentifiers</c>) without affecting
    /// any other entity.
    /// </summary>
    public enum ReactionEventOperation
    {
        Adding,
        Modifying,
        RemovingById,
        HardRemovingById,
        RetrievingById,

        // The narrow state-transition requests (design §9.7.1). Submitting moves the approval
        // status Draft → Submitted; Approving decides a submission. Each answers on its own
        // event address so it can be authorized in its own right (§8.6.1, §14.6).
        Submitting,
        Approving,
        Added,
        Modified,
        Removed,
        HardRemoved,

        // The facts the transitions publish. Submitted and Approved follow the operation;
        // Rejected follows the DECISION — an approve that rejects publishes Rejected, not
        // Approved.
        Submitted,
        Approved,
        Rejected
    }
}
