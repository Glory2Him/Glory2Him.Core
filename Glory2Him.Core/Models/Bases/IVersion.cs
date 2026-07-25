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
    public interface IVersion
    {
        /// <summary>
        /// Content item group identifier to group multiple versions of the same content item.
        /// </summary>
        Guid ContentItemGroupId { get; set; }

        /// <summary>
        /// Version number of the content item (required, defaults to 1).
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current instance represents the latest version.
        /// </summary>
        bool IsLatestVersion { get; set; }
    }
}
