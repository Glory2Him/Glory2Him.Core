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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// One content item the access broker is asked the effective <c>ContentItemSetting</c> of
    /// (§DOM6.4).
    ///
    /// <para>The type travels with the item because both tiers are keyed on it: an override is
    /// matched on the item and the type together, and the default that answers an item with no
    /// override is its type's.</para>
    /// </summary>
    public class ContentItemSettingKey
    {
        public required ContentType ContentType { get; init; }

        public required Guid ContentItemId { get; init; }
    }
}
