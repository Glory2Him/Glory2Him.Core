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
    /// A narrow native row naming one host and the far end an association points it at: the
    /// host's effective id on endpoint A and the key id on endpoint B. Nothing else about the
    /// row crosses back.
    /// </summary>
    public class AssociationPairKey
    {
        public Guid EntityAEffectiveId { get; set; }

        public Guid EntityBKeyId { get; set; }
    }
}
