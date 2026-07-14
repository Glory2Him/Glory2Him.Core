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
    }
}
