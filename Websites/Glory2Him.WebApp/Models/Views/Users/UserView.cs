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

namespace Glory2Him.WebApp.Models.Views.Users
{
    public class UserView
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool HasProfileImage { get; set; }
        public string? ImageVersion { get; set; }

        public string? ImageUrl =>
            HasProfileImage ? $"profile-image/{Id}?v={ImageVersion}" : null;
    }
}
