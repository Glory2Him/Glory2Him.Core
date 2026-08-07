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
    /// Which side of an anchor a sorted item lands on (design §9.7.1 rule 4).
    ///
    /// <para>Sorting takes an anchor and a side rather than a target index, because a pairwise
    /// swap cannot express a drag: dragging item 2 to position 7 shifts 3 through 7 up by one,
    /// whereas swapping 2 and 7 leaves 3 through 6 where they were — a visibly different list.
    /// With an anchor, nudging up is <c>(item, itemAbove, Before)</c>, nudging down is
    /// <c>(item, itemBelow, After)</c>, and an arbitrary drag is the item plus whatever it
    /// landed next to. Distance never enters the signature.</para>
    /// </summary>
    public enum SortPosition
    {
        Before = 0,
        After = 1
    }
}
