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

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// Identifies the mechanism by which the caller was authenticated.
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// The authentication type could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A human user authenticated interactively, for example via cookie or OpenID Connect.
        /// </summary>
        User = 1,

        /// <summary>
        /// A non-human caller authenticated using client credentials, for example a background job or AI worker.
        /// </summary>
        Machine = 2,

        /// <summary>
        /// A caller is acting on behalf of another subject using delegated access.
        /// </summary>
        Delegated = 3,

        /// <summary>
        /// An internal system process authenticated without a human or external client principal.
        /// </summary>
        System = 4
    }
}
