// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// What a decision settled, as recorded on the <c>Approval</c> row — the source of truth
    /// (design §9.8).
    /// </summary>
    public sealed class ApprovalOutcome
    {
        public required Guid ApprovalId { get; init; }

        public required EntityType EntityType { get; init; }

        public required Guid EntityId { get; init; }

        public required ApprovalStatus ApprovalStatus { get; init; }

        /// <summary>
        /// Whether the §8.5 conditions were waived. Taken from the decision's verdict, not from
        /// the request — a bypass asked for and not needed records none (§9.7.1 rule 3).
        /// </summary>
        public required bool IsApprovedByBypass { get; init; }

        public string? ApprovedByBypassReason { get; init; }

        /// <summary>
        /// The entity sync was REQUESTED, not confirmed. The command travels as an event, so
        /// §9.8's "must never diverge" is a steady-state invariant rather than a claim that both
        /// rows were written in one instant (§16.7.1).
        /// </summary>
        public required bool IsEntitySyncRequested { get; init; }
    }
}
