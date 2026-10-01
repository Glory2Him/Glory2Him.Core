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

namespace Glory2Him.Core.Models.Foundations.Associations
{
    /// <summary>
    /// One row of the grouped reaction count (§ARC16.8): how many counted rows pair one host,
    /// by its effective id, with one far end, by its key id. A narrow native row — it carries
    /// no association id and nothing about who gave the reaction, and a pair nobody gave has
    /// no row at all rather than a zero.
    /// </summary>
    public class AssociationPairCount
    {
        public Guid EntityAEffectiveId { get; set; }

        public Guid EntityBKeyId { get; set; }

        public int Count { get; set; }
    }
}
