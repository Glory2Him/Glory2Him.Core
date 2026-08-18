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

namespace Glory2Him.Core.Models.Bases
{
    /// <summary>
    /// Versioning for an entity whose amendments produce a new row rather than mutating the
    /// existing one (design §3.3, §3.4).
    ///
    /// <para><b><see cref="GroupId"/> was called <c>ContentItemGroupId</c>.</b> It was named for
    /// the first entity to carry it, and the name outlived that: every approvable entity is
    /// versioned, so a tag's version group would have been a "content item group id" on a row
    /// that has nothing to do with a content item.</para>
    /// </summary>
    public interface IVersion
    {
        /// <summary>
        /// Groups every version of one logical item. Constant across the whole version chain;
        /// minted once when version 1 is created and copied by each fork.
        ///
        /// <para>Not to be confused with an association's <c>EntityAGroupId</c> /
        /// <c>EntityBGroupId</c>, which are the group ids of the two rows it points AT. An
        /// association that implements this interface carries all three, and they answer
        /// different questions.</para>
        /// </summary>
        Guid GroupId { get; set; }

        /// <summary>
        /// Version number within the group (required, defaults to 1).
        /// </summary>
        int Version { get; set; }

        // There is no IsLatestVersion. The tip is DERIVED — the row with the
        // highest Version in the group — because a stored flag split the
        // "exactly one tip" invariant in two: a filtered index enforced at
        // most one, and application code was trusted for at least one. A fork
        // whose second write failed satisfied the index and left a group with
        // no tip at all, permanently uneditable (issue #265). Derived, that
        // state cannot be represented.

        /// <summary>
        /// Whether this row is the tip of the version chain — the row edits go to. Distinct from
        /// <c>IsPublished</c>, which marks the row the public reads; during a review window the
        /// two deliberately sit on different rows (design §3.4.1).
        /// </summary>
    }
}
