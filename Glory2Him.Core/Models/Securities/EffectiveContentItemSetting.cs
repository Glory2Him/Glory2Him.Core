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

using System;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// The <c>ContentItemSetting</c> row that wins for one content item — its own live override
    /// where it has one, its type's live default otherwise (§DOM6.4).
    ///
    /// <para>The item's id is carried beside the row because a default row answers every item of
    /// its type and names none of them.</para>
    ///
    /// <para>Init properties rather than a constructor, because the access broker projects into
    /// this inside the query it hands to storage, one subquery per item joined by a set operation.
    /// EF refuses a set operation over a constructor projection, which it can only run in memory
    /// after the fact.</para>
    /// </summary>
    public class EffectiveContentItemSetting
    {
        public required Guid ContentItemId { get; init; }

        public required ContentItemSetting ContentItemSetting { get; init; }
    }
}
