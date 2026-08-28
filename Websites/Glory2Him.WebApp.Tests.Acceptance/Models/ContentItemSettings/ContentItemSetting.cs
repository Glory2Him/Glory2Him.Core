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

namespace Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings
{
    /// <summary>
    /// The wire shape of a content interaction policy row.
    ///
    /// <para><b>No approval fields</b>, for the same reason <c>ApprovalSetting</c> has none:
    /// §7.5 entry 9 makes both approvable only <i>"if policy changes require approval"</i>, a
    /// conditional never taken up. The entity carries no <c>ApprovalStatus</c>,
    /// <c>IsPublished</c>, <c>PublishDate</c> or bypass pair.</para>
    /// </summary>
    public class ContentItemSetting
    {
        public Guid Id { get; set; }

        // The scope. ContentType alone is the per-type DEFAULT; a populated ContentItemId is an
        // override for one item. Each has its own filtered unique index, so exactly one row can
        // occupy either scope (§6.10 resolution depends on it, §12.5.2 business rules 3-4).
        //
        // ContentItemId carries NO foreign key — the column is a bare nullable Guid — so an
        // override can name an item that does not exist. Worth knowing before assuming storage
        // will catch a bad reference.
        public ContentType ContentType { get; set; }
        public Guid? ContentItemId { get; set; }

        // The submission form's shape for this type: whether a title and an author field exist at
        // all, and whether the type is offered to general users on the contribute page rather than
        // being admin/publisher-only.
        public bool HasTitle { get; set; }
        public bool HasAuthor { get; set; }
        public bool IsAvailableAsGeneralUserContribution { get; set; }

        // Display metadata for the type selector on the contribute page. All three are
        // optional. ContentTypeName caps at 50 characters, ContentTypeDescription at 500 (the
        // same cap DeletionReason carries — §6.5's other caller-supplied free text field).
        // ContentTypeIconCssClass names a Bootstrap Icons class (e.g. "bi-quote") — the same
        // icon set the rest of the UI already uses — rather than a literal emoji, since a CSS
        // class inherits color/hover/dark-mode and an emoji cannot.
        public string ContentTypeName { get; set; }
        public string ContentTypeDescription { get; set; }
        public string ContentTypeIconCssClass { get; set; }

        // The policy itself (§6.5). Each pair is "may this association type exist" and "should
        // it be rendered", which are separate questions: a page can keep its comments while
        // hiding them.
        public bool TagsAllowed { get; set; }
        public bool ShowTags { get; set; }
        public bool ReactionsAllowed { get; set; }
        public bool ShowReactions { get; set; }
        public bool LinksAllowed { get; set; }
        public bool ShowLinks { get; set; }
        public bool AttachmentsAllowed { get; set; }
        public bool ShowAttachments { get; set; }
        public bool CommentsAllowed { get; set; }
        public bool ShowComments { get; set; }
        public bool BibleReferenceAllowed { get; set; }
        public bool ShowBibleReferences { get; set; }
        public bool LimitReactionsToLoveOnly { get; set; }

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
