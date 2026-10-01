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
    /// The reader's own row on a host, as the personal-key lookup finds it — live or withdrawn
    /// (<c>Backend/Foundations/AssociationService.md §1</c>). It carries what the pair-keyed
    /// withdrawal branches on and nothing that could leak authorship: the lookup answers only for
    /// the signed caller, so there is no author to report.
    /// </summary>
    public class PersonalAssociationMatch
    {
        /// <summary>The reader's row's id.</summary>
        public Guid Id { get; set; }

        /// <summary>The key id of the reaction the row points at.</summary>
        public Guid EntityBKeyId { get; set; }

        /// <summary>Whether the row is withdrawn.</summary>
        public bool IsDeleted { get; set; }
    }
}
