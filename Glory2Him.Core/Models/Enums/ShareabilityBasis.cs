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
    /// Represents the basis on which a content item is permitted to be shared.
    /// </summary>
    public enum ShareabilityBasis
    {
        /// <summary>
        /// The contributor owns the content outright — it is their own words, story or work.
        /// </summary>
        Owned = 0,

        /// <summary>
        /// The contributor has been granted permission by the original owner to share the
        /// content. <see cref="Models.Foundations.ContentItems.ContentItem.SharePermission"/>
        /// optionally records the detail of that permission.
        /// </summary>
        PermissionGranted = 1,

        /// <summary>
        /// The content is in the public domain and requires no owner permission to share.
        /// </summary>
        PublicDomain = 2
    }
}
