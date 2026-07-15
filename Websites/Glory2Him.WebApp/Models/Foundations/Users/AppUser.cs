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
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Models.Foundations.Users
{
    public class AppUser : IdentityUser<Guid>
    {
        // Soft-delete / disable flag (Spec Section 11.9): a disabled account is locked out and
        // hidden from normal use but retained, preferred over a hard delete.
        public bool IsDisabled { get; set; }

        // Optional profile image, stored as a server-resized 256x256 WebP (see
        // ImageProcessingBroker). When null the UI falls back to an initials avatar.
        public byte[]? ProfileImage { get; set; }

        public string? ProfileImageContentType { get; set; }

        // Personal details. Name and Surname are required (stored NOT NULL with an empty-string
        // default for any legacy rows); DateOfBirth and PreferredName are optional.
        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string? PreferredName { get; set; }

        // A friendly display name: preferred name if given, otherwise "Name Surname", falling back
        // to the username when personal details are not yet completed.
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PreferredName))
                {
                    return PreferredName;
                }

                string fullName = $"{Name} {Surname}".Trim();

                return string.IsNullOrWhiteSpace(fullName)
                    ? UserName ?? string.Empty
                    : fullName;
            }
        }
    }
}
