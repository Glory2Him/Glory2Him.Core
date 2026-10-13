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

namespace Glory2Him.Core.Models.Foundations.LiveUpdates
{
    public sealed class LiveUpdate
    {
        /// <summary>
        /// The kind of read an open page may be holding that is now stale.
        /// </summary>
        public LiveUpdateType Type { get; set; }

        /// <summary>
        /// The content item version whose counts may have moved, for a
        /// <see cref="LiveUpdateType.ContentItemReactionsChanged"/> message; null otherwise.
        /// </summary>
        public Guid? ContentItemId { get; set; }
    }
}
