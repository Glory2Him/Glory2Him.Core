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
    /// Which <see cref="EntityType"/> members make an association personal, mirroring design
    /// §DOM4.10 rule 4. An association is personal where an endpoint's type is personal, and
    /// §DOM4.10 rule 5's chain starts here: this lookup decides whether <c>UserId</c> is set,
    /// which decides <c>IsPersonal</c>, which decides the approval tier and the unique index
    /// that governs the row.
    ///
    /// <para><b>Never answer this with an inline comparison against
    /// <see cref="EntityType.Reaction"/>.</b> The answer lives here and only here.</para>
    /// </summary>
    public static class EntityTypePersonalisation
    {
        // design §DOM4.10 rule 4: a missing row is a hard error, never a false default —
        // adding an EntityType member without adding it here is an incomplete change, and
        // EntityTypePersonalisationTests asserts every member is present
        private static readonly IReadOnlyDictionary<EntityType, bool> PersonalByEntityType =
            new Dictionary<EntityType, bool>
            {
                [EntityType.Reaction] = true,
                [EntityType.ContentItem] = false,
                [EntityType.Tag] = false,
                [EntityType.BibleReference] = false,
                [EntityType.Comment] = false,
                [EntityType.Link] = false,
                [EntityType.Attachment] = false,
                [EntityType.Association] = false
            };

        /// <summary>
        /// Whether an association with an endpoint of this entity type is personal.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The entity type has no declared answer. This is a hard error rather than a
        /// <c>false</c> default, which would file a forgotten member as editorial, under the
        /// wrong unique index and the wrong approval tier.
        /// </exception>
        public static bool IsPersonal(EntityType entityType)
        {
            if (PersonalByEntityType.TryGetValue(entityType, out bool isPersonal))
            {
                return isPersonal;
            }

            throw new NotSupportedException(
                $"Entity type '{entityType}' has no declared personalisation. " +
                $"Add it to {nameof(EntityTypePersonalisation)} (design §DOM4.10).");
        }
    }
}
