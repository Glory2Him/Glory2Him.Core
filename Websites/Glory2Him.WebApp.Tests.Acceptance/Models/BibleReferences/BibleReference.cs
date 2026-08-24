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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences
{
    public class BibleReference
    {
        public Guid Id { get; set; }

        // The canonical passage key, for example JHN.3.16.NIV. Required, capped at 50, unique
        // across non-deleted rows, and IMMUTABLE after creation — the foundation pins it against
        // storage on modify (design §12.3.1 rule 2a, §7.5.1 rule 4). It carries the translation
        // because Scripture is translation-specific.
        public string USFM { get; set; }

        // The three caller-editable content fields (§12.3.1 rule 2). Reference and Translation
        // are required, capped at 255 and 50; Scripture is optional.
        public string Reference { get; set; }
        public string Translation { get; set; }
        public string Scripture { get; set; }

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
