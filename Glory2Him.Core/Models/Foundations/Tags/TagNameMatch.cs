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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Foundations.Tags
{
    /// <summary>
    /// The row the read by name finds, projected to what the find-or-add branches on
    /// (<c>Backend/Foundations/TagService.TagSuggestions.md §3</c> rules 4 and 5): its id, its
    /// approval status and whether it is soft-deleted. It never leaves the member, whose answer
    /// is <see cref="TagNameResult"/>.
    /// </summary>
    public class TagNameMatch
    {
        /// <summary>The id of the row that holds the name.</summary>
        public Guid Id { get; set; }

        /// <summary>The row's approval status, which withholds a rejected tag.</summary>
        public ApprovalStatus ApprovalStatus { get; set; }

        /// <summary>Whether the row is soft-deleted, which withholds it too.</summary>
        public bool IsDeleted { get; set; }
    }
}
