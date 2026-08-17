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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews
{
    /// <summary>
    /// The wire shape of an approval review, as a caller outside the process sees it. It
    /// deliberately omits the <c>Approval</c> navigation the Core entity carries: a client only
    /// ever holds the parent's id, and the navigation would be a cycle on the wire.
    ///
    /// <para><c>StatusId</c> is the verdict — <c>Approved</c> or <c>Rejected</c> when a reviewer
    /// records one, and <c>Dismissed</c> only ever through the dismiss transition. It is an int
    /// here rather than the Core enum so the model stays a pure wire type.</para>
    /// </summary>
    public class ApprovalReview
    {
        public Guid Id { get; set; }
        public Guid ApprovalId { get; set; }
        public int StatusId { get; set; }
        public string Comment { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedWhen { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset UpdatedWhen { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedWhen { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletionReason { get; set; }
    }
}
