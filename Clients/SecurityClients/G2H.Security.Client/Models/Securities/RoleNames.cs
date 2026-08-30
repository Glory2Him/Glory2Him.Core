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

namespace G2H.Security.Client.Models.Securities
{
    /// <summary>
    /// The role-naming convention, and its single home. Who may review and who may publish is
    /// <b>composed</b> from the entity type rather than configured per policy row, so the
    /// composition has to live in exactly one place or the two encodings drift.
    ///
    /// <para>It lives here rather than in the consuming application because naming is an access
    /// concern: <see cref="G2H.Security.Client.Clients.Access.IAccessClient"/> decides eligibility
    /// by composing the name it expects and looking for it among the actor's roles, so the
    /// composer and the decision that depends on it are the same assembly.</para>
    ///
    /// <para>The segments are <c>string</c> rather than an enum because the entity and content
    /// types are the consuming application's vocabulary, and this package must not take a
    /// reference on it — the reference runs the other way.</para>
    /// </summary>
    public static class RoleNames
    {
        /// <summary>
        /// The block role. When present the user cannot contribute anywhere.
        ///
        /// <para>Alone among the capabilities this one stays <b>singular</b>, at every tier.
        /// The others name a group of people — reviewers, publishers, administrators — and a
        /// group takes the plural; <c>ReadOnly</c> names the state its holder is in, and has
        /// no sensible plural. That is a decision, not an oversight.</para>
        /// </summary>
        public const string ReadOnly = "ReadOnly";

        /// <summary>Global reviewers — may review any entity type.</summary>
        public const string Reviewers = "Reviewers";

        /// <summary>Global publishers — may approve and reject any entity type.</summary>
        public const string Publishers = "Publishers";

        /// <summary>Full access, including approval settings and bypass.</summary>
        public const string Administrators = "Administrators";

        // The capability segment of a granular role name. Plural, and always LAST:
        // `ContentItem-Story-Reviewers`, never `ContentItem-Reviewers-Story`. Eligibility is
        // decided by suffix match, so a name ending in anything else would not be recognised
        // as a review role at all and its holder would silently lose every capability the
        // suffix grants.
        public const string ReadOnlySuffix = "-ReadOnly";
        public const string ReviewersSuffix = "-Reviewers";
        public const string PublishersSuffix = "-Publishers";

        /// <summary>
        /// The entity-type-scoped block role, for example <c>Tag-ReadOnly</c>.
        /// </summary>
        public static string ReadOnlyFor(string entityType) =>
            $"{entityType}{ReadOnlySuffix}";

        /// <summary>
        /// The entity-type-scoped review role, for example <c>Tag-Reviewers</c> — the coarse
        /// tier, granting review over every instance of the type.
        /// </summary>
        public static string ReviewersFor(string entityType) =>
            $"{entityType}{ReviewersSuffix}";

        /// <summary>
        /// The entity-type-scoped publish role, for example <c>Tag-Publishers</c>.
        /// </summary>
        public static string PublishersFor(string entityType) =>
            $"{entityType}{PublishersSuffix}";

        /// <summary>
        /// The content-type-scoped review role, for example
        /// <c>ContentItem-Testimony-Reviewers</c> — the narrow tier, so a reviewer can be
        /// trusted with stories but not testimonies.
        /// </summary>
        public static string ReviewersFor(string entityType, string contentType) =>
            $"{entityType}-{contentType}{ReviewersSuffix}";

        /// <summary>
        /// The content-type-scoped publish role, for example
        /// <c>ContentItem-Testimony-Publishers</c>.
        /// </summary>
        public static string PublishersFor(string entityType, string contentType) =>
            $"{entityType}-{contentType}{PublishersSuffix}";

        // There is deliberately no ReadOnlyFor(entityType, contentType): the block role has no
        // content-type tier, and offering the composition would invent a role nothing issues
        // or checks.
    }
}
