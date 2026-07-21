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

using Glory2Him.WebApp.Models.Foundations.Users;

namespace Glory2Him.WebApp.Brokers.Profiles
{
    // Reads/writes the profile image using a short-lived DbContext from a factory, so avatar
    // lookups (rendered by several components at once — header, page, island) never contend on the
    // request-scoped Identity DbContext ("a second operation was started on this context").
    public interface IProfileImageBroker
    {
        ValueTask<AppUser?> SelectUserByIdAsync(Guid userId);

        ValueTask UpdateProfileImageAsync(Guid userId, byte[]? imageBytes, string? contentType);
    }
}
