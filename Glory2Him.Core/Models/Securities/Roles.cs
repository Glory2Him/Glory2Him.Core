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

using G2H.Security.Client.Models.Securities;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// Central catalogue of role names used for authorization checks (design §16.6).
    /// Three tiers by three capabilities, with no gaps: global roles apply across all entity
    /// types, granular <c>%EntityType%-*</c> roles apply to their own entity type, and
    /// <c>ContentItem-%ContentType%-*</c> narrows further to one content type — the one entity
    /// type carrying a content type (§18.6 rule 5).
    ///
    /// <para>The <c>ReadOnly</c> capability is the block, and it is a <b>veto</b> rather than a
    /// missing grant: within the scope it covers it wins over every other role,
    /// <see cref="Administrators"/> and the row's own author included, and outside that scope it
    /// is silent (§18.6 rule 2). Grants widen upward, blocks are absolute downward. It is also
    /// the one capability whose name stays singular: it names a state its holder is in, where the
    /// others name a group of people and take the plural.</para>
    ///
    /// <para>This is a <b>typed façade over the convention, not a second copy of it.</b> The
    /// spelling itself lives in <see cref="RoleNames"/> in <c>G2H.Security.Client</c>, because
    /// <c>IAccessClient</c> decides eligibility by composing the name it expects and looking
    /// for it among the actor's roles — so the composer and the decision that depends on it
    /// have to be the same assembly or they drift apart silently. What this class adds is the
    /// <see cref="EntityType"/> / <see cref="ContentType"/> overloads: the client takes strings
    /// because it must not reference this project, and every caller here holds an enum.</para>
    /// </summary>
    public static class Roles
    {
        public const string ReadOnly = RoleNames.ReadOnly;
        public const string Reviewers = RoleNames.Reviewers;
        public const string Publishers = RoleNames.Publishers;
        public const string Administrators = RoleNames.Administrators;

        public const string ContentItemReadOnly = "ContentItem-ReadOnly";
        public const string ContentItemReviewers = "ContentItem-Reviewers";
        public const string ContentItemPublishers = "ContentItem-Publishers";

        public const string TagReadOnly = "Tag-ReadOnly";
        public const string TagReviewers = "Tag-Reviewers";
        public const string TagPublishers = "Tag-Publishers";

        public const string ReactionReadOnly = "Reaction-ReadOnly";
        public const string ReactionReviewers = "Reaction-Reviewers";
        public const string ReactionPublishers = "Reaction-Publishers";

        public const string CommentReadOnly = "Comment-ReadOnly";
        public const string CommentReviewers = "Comment-Reviewers";
        public const string CommentPublishers = "Comment-Publishers";

        public const string BibleReferenceReadOnly = "BibleReference-ReadOnly";
        public const string BibleReferenceReviewers = "BibleReference-Reviewers";
        public const string BibleReferencePublishers = "BibleReference-Publishers";

        public const string LinkReadOnly = "Link-ReadOnly";
        public const string LinkReviewers = "Link-Reviewers";
        public const string LinkPublishers = "Link-Publishers";

        public const string AttachmentReadOnly = "Attachment-ReadOnly";
        public const string AttachmentReviewers = "Attachment-Reviewers";
        public const string AttachmentPublishers = "Attachment-Publishers";

        // Association has no scoped roles of its own (design §14.7, §18.6) —
        // authorization is derived from its two endpoint entity types instead.

        // The capability segment of a granular role name. Plural, and always LAST:
        // `ContentItem-Story-Reviewers`, never `ContentItem-Reviewers-Story`. Three approval
        // services identify a reviewer by suffix match, so a name ending in anything else
        // would not be recognised as a review role at all (design §18.6).
        public const string ReadOnlySuffix = RoleNames.ReadOnlySuffix;
        public const string ReviewersSuffix = RoleNames.ReviewersSuffix;
        public const string PublishersSuffix = RoleNames.PublishersSuffix;

        /// <summary>
        /// The entity-type-scoped block role, for example <c>Tag-ReadOnly</c>.
        ///
        /// <para>The constants above remain the canonical spelling and are not replaced by
        /// these helpers: a constant can appear in an xUnit <c>[InlineData]</c> attribute
        /// and a method call cannot, and roughly thirty test files depend on that. A
        /// parameterised test over every <c>EntityType</c> member asserts the two agree.</para>
        /// </summary>
        public static string ReadOnlyFor(EntityType entityType) =>
            RoleNames.ReadOnlyFor(entityType.ToString());

        /// <summary>
        /// The content-type-scoped block role, for example
        /// <c>ContentItem-Testimony-ReadOnly</c> — the narrow tier of the block, so a
        /// contributor can be sanctioned on testimonies alone and left free on stories. Only
        /// <c>ContentItem</c> has this granularity (design §18.6 rule 5).
        ///
        /// <para>It composes exactly like the two grants and reads the opposite way. A grant
        /// widens upward — the narrow one satisfies a check the coarse one satisfies too —
        /// while a block is absolute downward: within the scope it covers, no grant at any
        /// tier overrides it, <see cref="Administrators"/> included (§18.6 rule 2).</para>
        /// </summary>
        public static string ReadOnlyFor(EntityType entityType, ContentType contentType) =>
            RoleNames.ReadOnlyFor(entityType.ToString(), contentType.ToString());

        /// <summary>
        /// The entity-type-scoped review role, for example <c>Tag-Reviewers</c> — the coarse
        /// tier, granting review over every instance of the type.
        /// </summary>
        public static string ReviewersFor(EntityType entityType) =>
            RoleNames.ReviewersFor(entityType.ToString());

        /// <summary>
        /// The entity-type-scoped publish role, for example <c>Tag-Publishers</c>.
        /// </summary>
        public static string PublishersFor(EntityType entityType) =>
            RoleNames.PublishersFor(entityType.ToString());

        /// <summary>
        /// The content-type-scoped review role, for example
        /// <c>ContentItem-Testimony-Reviewers</c> — the narrow tier, so a reviewer can be
        /// trusted with stories but not testimonies. Only <c>ContentItem</c> has this
        /// granularity; no other entity type carries a content type (design §18.6 rule 5).
        /// </summary>
        public static string ReviewersFor(EntityType entityType, ContentType contentType) =>
            RoleNames.ReviewersFor(entityType.ToString(), contentType.ToString());

        /// <summary>
        /// The content-type-scoped publish role, for example
        /// <c>ContentItem-Testimony-Publishers</c>.
        /// </summary>
        public static string PublishersFor(EntityType entityType, ContentType contentType) =>
            RoleNames.PublishersFor(entityType.ToString(), contentType.ToString());
    }
}
