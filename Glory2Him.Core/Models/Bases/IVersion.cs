// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
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
        bool G2HatestVersion { get; set; }
    }
}
