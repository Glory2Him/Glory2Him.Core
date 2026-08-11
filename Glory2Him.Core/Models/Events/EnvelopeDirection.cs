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

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// Which leg of the exchange an envelope belongs to. Bound into the integrity signature so a
    /// signed reply can never be replayed as a request: a reply carries the original caller's
    /// <c>SecurityContext</c> verbatim and a fresh <c>EventId</c>, so without this discriminator a
    /// signed reply would be a structurally valid signed request for the same event name.
    /// </summary>
    public enum EnvelopeDirection
    {
        /// <summary>An event published to an address — a request or a fact.</summary>
        Request = 0,

        /// <summary>A handler's response, serialized back into the delivery row.</summary>
        Reply = 1
    }
}
