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
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Foundations.ContentItemSettings
{
    /// <summary>
    /// Represents configurable settings for a content item type or specific entity instance.
    /// </summary>
    public class ContentItemSetting : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the content item setting.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The content type this setting applies to.
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// The content type name.
        /// </summary>
        public string? ContentTypeName { get; set; } = string.Empty;

        /// <summary>
        /// The content type description.
        /// </summary>
        public string? ContentTypeDescription { get; set; } = string.Empty;

        /// <summary>
        /// The Bootstrap Icons CSS class (e.g. "bi-quote") used to represent this content type
        /// in the UI.
        /// </summary>
        public string? ContentTypeIconCssClass { get; set; } = string.Empty;

        /// <summary>
        /// The position this content type takes wherever the types are presented as a list —
        /// the contribute page's type picker above all. Lower sorts first; ties fall back to
        /// whatever order the rows arrived in.
        ///
        /// <para>The default of 1000 sits past every curated value the seed writes, so a row
        /// added without a considered order lands after the types somebody chose the order of
        /// rather than in front of them. Must be zero or greater — the foundation rejects a
        /// negative, which no surface reading this could mean anything by.</para>
        /// </summary>
        public int SortOrder { get; set; } = 1000;

        /// <summary>
        /// Optional identifier for a specific content item.
        /// If left blank, this setting applies as the default for the content type.
        /// When provided, it overrides the default.
        /// </summary>
        public Guid? ContentItemId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a title field is present.
        /// </summary>
        public bool HasTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an author field is present.
        /// </summary>
        public bool HasAuthor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a non-admin / non-publisher
        /// can contribute on this type.
        /// </summary>
        public bool IsAvailableAsGeneralUserContribution { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the maximum length of the title field, if applicable.
        /// </summary>
        public int? MaxTitleLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the maximum length of the author field, if applicable.
        /// </summary>
        public int? MaxAuthorLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the maximum length of the content field, if applicable.
        /// </summary>
        public int? MaxContentLength { get; set; }

        // --------------------
        // Tag Settings
        // --------------------

        /// <summary>
        /// Indicates whether new tags can be created.
        /// </summary>
        public bool TagsAllowed { get; set; }

        /// <summary>
        /// Indicates whether tags should be displayed.
        /// </summary>
        public bool ShowTags { get; set; }

        // --------------------
        // Reaction Settings
        // --------------------

        /// <summary>
        /// Indicates whether new reactions can be created.
        /// </summary>
        public bool ReactionsAllowed { get; set; }

        /// <summary>
        /// Indicates whether reactions should be displayed.
        /// </summary>
        public bool ShowReactions { get; set; }

        // --------------------
        // Link Settings
        // --------------------

        /// <summary>
        /// Indicates whether new links can be created.
        /// </summary>
        public bool LinksAllowed { get; set; }

        /// <summary>
        /// Indicates whether links should be displayed.
        /// </summary>
        public bool ShowLinks { get; set; }

        // --------------------
        // Attachment Settings
        // --------------------

        /// <summary>
        /// Indicates whether new attachments can be created.
        /// </summary>
        public bool AttachmentsAllowed { get; set; }

        /// <summary>
        /// Indicates whether attachments should be displayed.
        /// </summary>
        public bool ShowAttachments { get; set; }

        // --------------------
        // Comment Settings
        // --------------------

        /// <summary>
        /// Indicates whether new comments can be created.
        /// </summary>
        public bool CommentsAllowed { get; set; }

        /// <summary>
        /// Indicates whether comments should be displayed.
        /// </summary>
        public bool ShowComments { get; set; }

        // --------------------
        // Bible Reference Settings
        // --------------------

        /// <summary>
        /// Indicates whether new Bible references can be created.
        /// </summary>
        public bool BibleReferenceAllowed { get; set; }

        /// <summary>
        /// Indicates whether Bible references should be displayed.
        /// </summary>
        public bool ShowBibleReferences { get; set; }

        /// <summary>
        /// Indicates whether only the love reaction is permitted (favourite-style behaviour).
        /// When true, only the designated love reaction may be associated with content items of this type.
        /// </summary>
        public bool LimitReactionsToLoveOnly { get; set; }

        /// <summary>
        /// User identifier for who created the content item.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the content item.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the content item.
        /// </summary>
        public string? DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the content item was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content item is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }
    }
}
