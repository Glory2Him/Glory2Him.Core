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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems
{
    public class ContentItem
    {
        public Guid Id { get; set; }

        // Set at creation and never changed (§12.4.1 business rule 7a): different content types
        // carry different validation rules, so an item cannot be relabelled into a type its
        // content was never checked against. Pinned in the foundation and dropped from the
        // processing service's permitted map — defence in depth rather than one gate.
        public ContentType ContentType { get; set; }

        // The caller-editable content (§12.4.1 rule 7). Title and Author are optional.
        public string Title { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }

        // The basis on which the contributor may share this content, and the optional detail
        // recorded when that basis is PermissionGranted. SharePermission caps at 500 characters,
        // the same cap DeletionReason and ApprovedByBypassReason carry.
        public ShareabilityBasis ShareabilityBasis { get; set; }
        public string SharePermission { get; set; }

        // Control fields the caller never supplies (§12.4.1 rule 6). They appear here because the
        // API RETURNS them and the assertions read them — not because a request may carry them.
        // ContentHash is derived from Content, and GroupId plus Version name the row within its
        // version group; the tip is the highest non-deleted Version rather than a stored flag
        // (§3.4.1).
        public string ContentHash { get; set; }
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
