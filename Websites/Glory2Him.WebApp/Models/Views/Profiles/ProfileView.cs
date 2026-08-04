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

namespace Glory2Him.WebApp.Models.Views.Profiles
{
    public class ProfileView
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool HasProfileImage { get; set; }

        // Short content hash used to bust the browser cache when the image changes.
        public string? ImageVersion { get; set; }

        // Resolves to the serving endpoint when an image is set, otherwise null (initials fallback).
        public string? ImageUrl =>
            HasProfileImage ? $"Profile-Image/{Id}?v={ImageVersion}" : null;
    }
}
