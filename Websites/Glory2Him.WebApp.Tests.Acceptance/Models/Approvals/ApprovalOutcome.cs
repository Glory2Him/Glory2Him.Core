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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Approvals
{
    /// <summary>
    /// The wire shape of a decision's outcome (§16.7.3): what the round became. A pure wire
    /// type; the enums are ints because that is what crosses.
    /// </summary>
    public class ApprovalOutcome
    {
        public Guid ApprovalId { get; set; }
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public int ApprovalStatus { get; set; }
        public bool IsApprovedByBypass { get; set; }
        public string ApprovedByBypassReason { get; set; }
        public bool IsEntitySyncRequested { get; set; }
    }
}
