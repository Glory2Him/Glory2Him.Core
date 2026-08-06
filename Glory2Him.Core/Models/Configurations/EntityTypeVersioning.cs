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
using System.Collections.Generic;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Configurations
{
    /// <summary>
    /// The publication model of every <see cref="EntityType"/>, mirroring design §7.5.1.
    /// <c>Versioned</c> means an amendment to an approved row produces a new row and the
    /// previously published row stays live until the new one is approved; <c>Single-Row</c>
    /// means the row that is edited <b>is</b> the published row.
    ///
    /// <para><b>Never probe the entity for <c>IVersion</c> to answer this.</b> Runtime shape
    /// is not a stable discriminator and this repository has proved it twice: design §5.1
    /// and §5.2 described <c>Tag</c> and <c>Reaction</c> as carrying versioning properties
    /// that neither type ever implemented, and <c>BibleReference</c> dropped <c>IVersion</c>
    /// while its storage configuration and validations kept referencing the properties. A
    /// probe would have silently changed behaviour; a lookup fails loudly instead.</para>
    /// </summary>
    public static class EntityTypeVersioning
    {
        // design §7.5.1 rule 2: a missing row is a hard error, never a default — adding an
        // EntityType member without adding it here is an incomplete change, and
        // EntityTypeVersioningTests asserts every member is present
        private static readonly IReadOnlyDictionary<EntityType, bool> VersionedByEntityType =
            new Dictionary<EntityType, bool>
            {
                [EntityType.ContentItem] = true,
                [EntityType.Link] = true,
                [EntityType.Attachment] = true,
                [EntityType.BibleReference] = false,
                [EntityType.Tag] = false,
                [EntityType.Reaction] = false,
                [EntityType.Comment] = false,
                [EntityType.Association] = false
            };

        /// <summary>
        /// Whether an amendment to this entity type produces a new row.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The entity type has no declared publication model. This is a hard error rather
        /// than a default, so an entity type added without a decision fails on first use
        /// instead of silently picking a branch of the approval workflow.
        /// </exception>
        public static bool IsVersioned(EntityType entityType)
        {
            if (VersionedByEntityType.TryGetValue(entityType, out bool isVersioned))
            {
                return isVersioned;
            }

            throw new NotSupportedException(
                $"Entity type '{entityType}' has no declared publication model. " +
                $"Add it to {nameof(EntityTypeVersioning)} (design §7.5.1).");
        }

        /// <summary>
        /// The scope an association endpoint of this entity type takes. A non-versioned
        /// endpoint has exactly one row, so <see cref="Scope.AllVersions"/> would be a
        /// distinction without a difference — it resolves to
        /// <see cref="Scope.ThisVersionOnly"/>. A versioned endpoint defaults to
        /// <see cref="Scope.AllVersions"/>, which is what a tag on a story means: it
        /// survives the story being amended.
        /// </summary>
        public static Scope DefaultScopeFor(EntityType entityType) =>
            IsVersioned(entityType)
                ? Scope.AllVersions
                : Scope.ThisVersionOnly;
    }
}
