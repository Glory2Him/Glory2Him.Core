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
using System.IO;
using System.Threading.Tasks;
using Glory2Him.WebApp.Brokers.Images;
using Glory2Him.WebApp.Models.Views.Profiles;

namespace Glory2Him.WebApp.Services.Views.Profiles
{
    public interface IProfileViewService
    {
        ValueTask<ProfileView> RetrieveProfileByIdAsync(Guid userId);

        // Validates (size + image content type), resizes to a square WebP avatar, and persists it.
        ValueTask SetProfileImageAsync(
            Guid userId,
            Stream imageStream,
            long byteLength,
            string contentType);

        ValueTask RemoveProfileImageAsync(Guid userId);

        // Returns the stored avatar bytes for the serving endpoint, or null when none is set.
        ValueTask<ProcessedImage?> RetrieveProfileImageAsync(Guid userId);
    }
}
