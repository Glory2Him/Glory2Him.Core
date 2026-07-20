// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// The operations a <c>ContentType</c> event can represent — requests (present tense:
    /// <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="RetrievingById"/>) answered by responder handlers, and facts (past tense:
    /// <see cref="Added"/>, <see cref="Modified"/>, <see cref="Removed"/>) published by
    /// the service after the work is done. Every operation maps to its own event address (for
    /// example <c>ContentType-Adding</c>) and composes the stored event name (for example
    /// <c>"ContentTypeAdding"</c>). Entity-specific operations may be appended here (with a
    /// matching event address in <c>EventBrokerIdentifiers</c>) without affecting any other
    /// entity.
    /// </summary>
    public enum ContentTypeEventOperation
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
