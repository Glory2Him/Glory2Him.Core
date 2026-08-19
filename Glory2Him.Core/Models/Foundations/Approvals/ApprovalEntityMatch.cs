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

namespace Glory2Him.Core.Models.Foundations.Approvals
{
    /// <summary>
    /// The non-leaking result of the unfiltered <c>(EntityType, EntityId)</c> lookup
    /// (design §9.7.2 rule 3, §12.4.4 BR14).
    ///
    /// <para>The retrieve-or-create flow must see rows the caller-facing reads hide, and for a
    /// different reason than the association probe: <c>UX_Approvals_EntityType_EntityId</c> is
    /// <b>not</b> filtered on <c>IsDeleted</c>, so a soft-deleted approval still occupies the
    /// key. A visibility-filtered lookup answers "does not exist" for a key that does exist, and
    /// the insert that answer invites can never succeed. So the lookup runs unfiltered and the
    /// flow reinstates the row in place instead.</para>
    ///
    /// <para>The row body never crosses back. This projection carries only what the
    /// orchestration branches on — the id it will reinstate or move, the status that selects the
    /// branch, and the soft-delete flag that decides between reinstate and update.</para>
    /// </summary>
    public class ApprovalEntityMatch
    {
        /// <summary>The matched row's id — what the flow reinstates or transitions.</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The matched row's status, which selects the branch: a `Draft` approval ends the added
        /// flow, a `Submitted` one is evaluated, and a terminal one is an override.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; }

        /// <summary>
        /// Whether the matched row is soft-deleted. A closed approval is **reinstated in place**,
        /// never re-inserted (§12.4.4 BR14) — the unique index spans deleted rows, so a second
        /// insert on the same key cannot succeed.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
