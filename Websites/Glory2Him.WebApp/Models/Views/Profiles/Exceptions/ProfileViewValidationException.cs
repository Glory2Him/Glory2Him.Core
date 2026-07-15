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

using Xeptions;

namespace Glory2Him.WebApp.Models.Views.Profiles.Exceptions
{
    // Thrown when a profile-image upload fails a UI-facing rule (e.g. too large, not an image).
    public class ProfileViewValidationException : Xeption
    {
        public ProfileViewValidationException(string message)
            : base(message)
        { }
    }
}
