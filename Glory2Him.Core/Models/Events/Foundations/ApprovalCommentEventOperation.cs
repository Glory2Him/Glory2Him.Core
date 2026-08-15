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
    /// The operations a <c>ApprovalComment</c> event can represent — requests (present tense:
    /// <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="HardRemovingById"/>, <see cref="RetrievingById"/>) answered by responder
    /// handlers, and facts (past tense: <see cref="Added"/>, <see cref="Modified"/>,
    /// <see cref="Removed"/>, <see cref="HardRemoved"/>) published by the service after the
    /// work is done. Every request operation maps to its own event address (for example
    /// <c>ApprovalComment-Adding</c>) and composes the stored event name (for example
    /// <c>"ApprovalCommentAdding"</c>). <see cref="HardRemoved"/> shares the
    /// <see cref="Removed"/> event address and is distinguished purely by its event name
    /// (<c>"ApprovalCommentHardRemoved"</c>). Entity-specific operations may be appended here
    /// (with a matching event address in <c>EventBrokerIdentifiers</c>) without affecting
    /// any other entity.
    /// </summary>
    public enum ApprovalCommentEventOperation
    {
        Adding,
        Modifying,
        RemovingById,
        HardRemovingById,
        RetrievingById,

        // The narrow state-transition request (design §9.7.1). Resolving owns
        // <c>IsResolved</c> and nothing else: it records whether a comment is settled — whether
        // it still requires something before the approval can proceed — which is a different act
        // from correcting the words. It answers on its own address because it is the one comment
        // operation an <c>Admin</c> may perform on another person's row.
        //
        // Not every comment asks for anything: an observation, or a reviewer recording rationale
        // for others to see, is created settled and never blocks. So a Resolving request is not
        // "a question was answered" — it is a settled/outstanding transition, and it runs in both
        // directions.
        Resolving,
        Added,
        Modified,
        Removed,
        HardRemoved,

        // The fact the resolution transition publishes. Carries the whole row, so a consumer
        // reads the new IsResolved off the content rather than inferring it from the address.
        Resolved
    }
}
