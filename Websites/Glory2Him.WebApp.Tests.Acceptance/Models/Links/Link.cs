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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Links
{
    public class Link
    {
        public Guid Id { get; set; }

        // The caller-editable content (§12.4.2 responsibility 4). All three are required; Name
        // is capped at 255.
        //
        // NOTE what is absent, because a reader arriving from ContentItem will look for it:
        // there is no ContentType, no Title/Author/Content and no ContentHash. §3.4.2's
        // duplicate-content rule is keyed on (ContentType, ContentHash) and a link carries
        // neither — two links to the same URL are a legitimate pair, the same article cited from
        // two stories under two names (§12.4.2).
        public string Name { get; set; }
        public string Url { get; set; }
        public string LinkType { get; set; }

        // Control fields the caller never supplies (§12.4.2 business rule 6). They appear here
        // because the API RETURNS them and the assertions read them. The tip is the highest
        // non-deleted Version rather than a stored flag (§3.4.1).
        public Guid GroupId { get; set; }
        public int Version { get; set; }

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
