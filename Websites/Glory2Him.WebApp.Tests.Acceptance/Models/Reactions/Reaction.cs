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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Reactions
{
    public class Reaction
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // The second caller-editable content field, and the one thing that makes this model
        // more than a renamed Tag (design §12.3.1 rule 2, §5.2). Required, capped at 16.
        public string UnicodeEmoji { get; set; }

        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedWhen { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset UpdatedWhen { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedWhen { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletionReason { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public bool IsApprovedByBypass { get; set; }
        public string ApprovedByBypassReason { get; set; }
    }
}
