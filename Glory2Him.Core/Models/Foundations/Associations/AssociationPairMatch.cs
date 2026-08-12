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

namespace Glory2Him.Core.Models.Foundations.Associations
{
    /// <summary>
    /// The non-leaking result of the unfiltered canonical-pair lookup (design §7.4). The
    /// retrieve-or-add flow must see rows the read posture hides from the submitting user — a
    /// pending or rejected row belonging to someone else, or a soft-deleted one — so the lookup
    /// runs over the unfiltered store; but the row body must never cross back to the caller,
    /// because it carries another user's <see cref="CreatedBy"/> and the read posture reports
    /// non-public rows to non-owners as not-found. This projection carries only what the
    /// orchestration needs to BRANCH — the id it will echo, the approval state, and the
    /// soft-delete provenance the resurrect rule (§10.4) turns on — and nothing else.
    /// </summary>
    public class AssociationPairMatch
    {
        /// <summary>The matched row's id — the only field the orchestration echoes to the caller.</summary>
        public Guid Id { get; set; }

        /// <summary>The matched row's approval state, which selects the Created / AlreadyPending / AlreadyApproved branch.</summary>
        public ApprovalStatus ApprovalStatus { get; set; }

        /// <summary>Whether the matched row is soft-deleted, which selects the resurrect branch.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>The matched row's author. A soft-deleted row may only be resurrected by its own author.</summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Who soft-deleted the row, if any. When it differs from <see cref="CreatedBy"/> the
        /// deletion was a moderator takedown and the row must NOT be resurrected — otherwise a
        /// contributor could launder a takedown by resubmitting.
        /// </summary>
        public string? DeletedBy { get; set; }
    }
}
