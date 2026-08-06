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

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// Central catalogue of role names used for authorization checks (design §16.6).
    /// Global roles apply across all entity types; granular <c>%EntityType%-*</c> roles
    /// grant or block capability only for their own entity type. The global
    /// <see cref="ReadOnly"/> role is the block role — when present it wins over every
    /// other role and the user cannot contribute anywhere.
    /// </summary>
    public static class Roles
    {
        public const string ReadOnly = "ReadOnly";
        public const string Reviewer = "Reviewer";
        public const string Publisher = "Publisher";
        public const string Admin = "Admin";

        public const string ContentItemReadOnly = "ContentItem-ReadOnly";
        public const string ContentItemReviewer = "ContentItem-Reviewer";
        public const string ContentItemPublisher = "ContentItem-Publisher";

        public const string TagReadOnly = "Tag-ReadOnly";
        public const string TagReviewer = "Tag-Reviewer";
        public const string TagPublisher = "Tag-Publisher";

        public const string ReactionReadOnly = "Reaction-ReadOnly";
        public const string ReactionReviewer = "Reaction-Reviewer";
        public const string ReactionPublisher = "Reaction-Publisher";

        public const string CommentReadOnly = "Comment-ReadOnly";
        public const string CommentReviewer = "Comment-Reviewer";
        public const string CommentPublisher = "Comment-Publisher";

        public const string BibleReferenceReadOnly = "BibleReference-ReadOnly";
        public const string BibleReferenceReviewer = "BibleReference-Reviewer";
        public const string BibleReferencePublisher = "BibleReference-Publisher";

        public const string LinkReadOnly = "Link-ReadOnly";
        public const string LinkReviewer = "Link-Reviewer";
        public const string LinkPublisher = "Link-Publisher";

        // Association has no scoped roles of its own (design §14.7, §18.6) —
        // authorization is derived from its two endpoint entity types instead.
    }
}
