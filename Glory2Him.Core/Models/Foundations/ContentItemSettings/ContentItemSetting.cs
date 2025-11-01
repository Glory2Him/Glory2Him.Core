// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;

namespace Glory2Him.Core.Models.Foundations.ContentItemSettings
{
    /// <summary>
    /// Represents configurable settings for a content item type or specific entity instance.
    /// </summary>
    public class ContentItemSetting
    {
        /// <summary>
        /// Primary key identifier for the content item setting.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier for the content type this setting applies to.
        /// </summary>
        public string ContentTypeId { get; set; } = string.Empty;

        /// <summary>
        /// Optional identifier for a specific content item.
        /// If left blank, this setting applies as the default for the content type.
        /// When provided, it overrides the default.
        /// </summary>
        public Guid? ContentItemId { get; set; }

        // --------------------
        // Tag Settings
        // --------------------

        /// <summary>
        /// Indicates whether new tags can be created.
        /// </summary>
        public bool TagsAllowed { get; set; }

        /// <summary>
        /// Indicates whether tag associations require approval.
        /// </summary>
        public bool TagAssociationsRequireApproval { get; set; }

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
        /// Indicates whether reaction associations require approval.
        /// </summary>
        public bool ReactionAssociationsRequireApproval { get; set; }

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
        /// Indicates whether link associations require approval.
        /// </summary>
        public bool LinkAssociationsRequireApproval { get; set; }

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
        /// Indicates whether attachment associations require approval.
        /// </summary>
        public bool AttachmentAssociationsRequireApproval { get; set; }

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
        /// Indicates whether comment associations require approval.
        /// </summary>
        public bool CommentAssociationsRequireApproval { get; set; }

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
        /// Indicates whether Bible reference associations require approval.
        /// </summary>
        public bool BibleReferenceAssociationsRequireApproval { get; set; }

        /// <summary>
        /// Indicates whether Bible references should be displayed.
        /// </summary>
        public bool ShowBibleReferences { get; set; }
    }
}
