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
    }
}
