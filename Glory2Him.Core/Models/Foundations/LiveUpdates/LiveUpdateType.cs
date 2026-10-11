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

namespace Glory2Him.Core.Models.Foundations.LiveUpdates
{
    /// <summary>
    /// What a live update says is stale. It crosses the wire as a number, so members are
    /// appended and never renumbered, and none is 0.
    /// </summary>
    public enum LiveUpdateType
    {
        ContentItemSettingChanged = 1,
        ContentItemReactionsChanged = 2
    }
}
