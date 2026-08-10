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

namespace Glory2Him.Core.Models.Bases
{
    public interface IApproval
    {
        /// <summary>
        /// The date and time when the content item was published. 
        /// This is nullable to allow for drafts that have not yet been published.
        /// </summary>
        DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content item is published.
        /// </summary>
        bool IsPublished { get; set; }

        /// <summary>
        /// A denormalized field to indicate if the content item has been approved.
        /// This is used to optimize queries for approved content items without
        /// needing to join with the approvals table.
        /// </summary>
        ApprovalStatus ApprovalStatus { get; set; }

        /// <summary>
        /// Indicates whether the approval conditions were bypassed to reach the current
        /// <see cref="ApprovalStatus"/>. Denormalized from the approval record for the same
        /// reason the status is: so "what was published without meeting its conditions" is a
        /// query rather than a join.
        ///
        /// <para><b>Derived on write and never accepted from a caller.</b> It exists to record
        /// that the conditions were waived, and a caller who could set it could equally clear
        /// it — un-recording the one event the field is here to capture. The approve operation
        /// writes it from the access decision, not from its input, which is why it is an
        /// exception to the rule that approve copies the caller's <c>IApproval</c> values
        /// (design §9.7.1 rule 3).</para>
        /// </summary>
        bool IsApprovedByBypass { get; set; }

        /// <summary>
        /// Why the approval conditions were waived. Populated only when
        /// <see cref="IsApprovedByBypass"/> is true, and cleared with it.
        ///
        /// <para>Derived on write from the same decision, for the same reason. A bypass is only
        /// tolerable because it leaves a record, and an unexplained one records nothing worth
        /// reading.</para>
        /// </summary>
        string? ApprovedByBypassReason { get; set; }
    }
}
