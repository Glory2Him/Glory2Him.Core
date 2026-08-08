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
    /// Global roles apply across all entity types; granular <c>%EntityType%-*</c> roles
    /// grant or block capability only for their own entity type. The global
    /// <see cref="ReadOnly"/> role is the block role — when present it wins over every
    /// other role and the user cannot contribute anywhere.
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
        public const string Reviewer = RoleNames.Reviewer;
        public const string Publisher = RoleNames.Publisher;
        public const string Admin = RoleNames.Admin;

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

        public const string AttachmentReadOnly = "Attachment-ReadOnly";
        public const string AttachmentReviewer = "Attachment-Reviewer";
        public const string AttachmentPublisher = "Attachment-Publisher";

        // Association has no scoped roles of its own (design §14.7, §18.6) —
        // authorization is derived from its two endpoint entity types instead.

        // The capability segment of a granular role name. Singular, and always LAST:
        // `ContentItem-Story-Reviewer`, never `ContentItem-Reviewer-Story`. Three approval
        // services identify a reviewer by suffix match, so a name ending in anything else
        // would not be recognised as a review role at all (design §18.6).
        public const string ReadOnlySuffix = RoleNames.ReadOnlySuffix;
        public const string ReviewerSuffix = RoleNames.ReviewerSuffix;
        public const string PublisherSuffix = RoleNames.PublisherSuffix;

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
        /// The entity-type-scoped review role, for example <c>Tag-Reviewer</c> — the coarse
        /// tier, granting review over every instance of the type.
        /// </summary>
        public static string ReviewerFor(EntityType entityType) =>
            RoleNames.ReviewerFor(entityType.ToString());

        /// <summary>
        /// The entity-type-scoped publish role, for example <c>Tag-Publisher</c>.
        /// </summary>
        public static string PublisherFor(EntityType entityType) =>
            RoleNames.PublisherFor(entityType.ToString());

        /// <summary>
        /// The content-type-scoped review role, for example
        /// <c>ContentItem-Testimony-Reviewer</c> — the narrow tier, so a reviewer can be
        /// trusted with stories but not testimonies. Only <c>ContentItem</c> has this
        /// granularity; no other entity type carries a content type (design §18.6 rule 5).
        /// </summary>
        public static string ReviewerFor(EntityType entityType, ContentType contentType) =>
            RoleNames.ReviewerFor(entityType.ToString(), contentType.ToString());

        /// <summary>
        /// The content-type-scoped publish role, for example
        /// <c>ContentItem-Testimony-Publisher</c>.
        /// </summary>
        public static string PublisherFor(EntityType entityType, ContentType contentType) =>
            RoleNames.PublisherFor(entityType.ToString(), contentType.ToString());

        // There is deliberately no ReadOnlyFor(EntityType, ContentType): the block role has
        // no content-type tier (design §18.6 lists only -Reviewer and -Publisher there), and
        // offering the composition would invent a role nothing issues or checks.
    }
}
