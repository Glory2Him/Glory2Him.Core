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

namespace Glory2Him.Core.Models.Enums
{
    /// <summary>
    /// Classifies a <see cref="ContentItem"/>. A content type carries its own validation
    /// rules in the foundation service, so it cannot be created at runtime without a code
    /// change — hence a fixed enum rather than a database-backed entity (design §3.6).
    ///
    /// <para>Values are persisted as strings (design §10.2) and are <b>append-only</b>:
    /// never renumbered, never reused. A content item's <c>ContentType</c> is denormalised
    /// onto association rows and composes content-type-scoped role names (design §18.6), so
    /// a rename or renumber silently reassigns authority and identity that already exists.
    /// Add a new member at the end; never repurpose or remove one that is in use.</para>
    /// </summary>
    public enum ContentType
    {
        /// <summary>A short, standalone scripture-anchored quote.</summary>
        Quote = 0,

        /// <summary>A long-form narrative or illustration piece.</summary>
        Story = 1,

        /// <summary>A first-person account of a life experience.</summary>
        Testimony = 2,

        /// <summary>A short, standalone devotional.</summary>
        Devotional = 3,

        /// <summary>A long-form study on something specific in the Bible.</summary>
        BibleStudy = 4,

        /// <summary>A blog post.</summary>
        BlogPost = 5,

        /// <summary>A verse image.</summary>
        ///
        /// <remarks>
        /// RENAMED FROM <c>Verses</c>, which is the one thing the note above says never
        /// happens — so it was done the only way that keeps the rule's promise. The VALUE is
        /// untouched (6), and every place the old name was persisted or composed into was
        /// rewritten in the same release: the string column on ContentItems,
        /// ContentItemSettings, ApprovalSettings and both endpoints of Associations by
        /// <c>RenameVersesContentTypeToVerseImage</c>, and the
        /// <c>ContentItem-Verses-*</c> role names by the SecurityDbContext migration of the
        /// same name. Nothing was reassigned: the member means what it always meant, and now
        /// says so. Do not take this as licence to rename another — it cost two migrations.
        /// </remarks>
        VerseImage = 6,

        /// <summary>
        /// An ordered collection of related content items — excluded from feed projections
        /// (design §3.8). Numbered apart from the standalone content types above pending
        /// design §3.9, which revisits whether this is distinct from <see cref="Topic"/>.
        /// </summary>
        Series = 100,

        /// <summary>
        /// A grouping content item — excluded from feed projections (design §3.8). Numbered
        /// apart from the standalone content types above pending design §3.9, which revisits
        /// whether this is distinct from <see cref="Series"/>.
        /// </summary>
        Topic = 200
    }
}
