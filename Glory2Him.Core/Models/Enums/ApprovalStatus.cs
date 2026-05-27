// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// Represents the current status of an approval workflow.
    /// </summary>
    public enum ApprovalStatus
    {
        /// <summary>
        /// The entity is in draft state and not yet submitted for review.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// The entity has been submitted and is awaiting one or more reviews.
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// The entity has received all required approvals and is now approved.
        /// </summary>
        Approved = 2,

        /// <summary>
        /// The entity has been rejected during the approval process.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// The entity was previously approved but has since been dismissed, 
        /// indicating that the approval is no longer valid and may require re-submission for review.
        /// </summary>
        Dismissed = 4,
    }
}
