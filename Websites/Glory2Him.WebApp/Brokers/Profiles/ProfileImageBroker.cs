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

using Glory2Him.WebApp.Data;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Brokers.Profiles
{
    public sealed class ProfileImageBroker : IProfileImageBroker
    {
        private readonly IDbContextFactory<SecurityDbContext> dbContextFactory;

        public ProfileImageBroker(IDbContextFactory<SecurityDbContext> dbContextFactory) =>
            this.dbContextFactory = dbContextFactory;

        public async ValueTask<AppUser?> SelectUserByIdAsync(Guid userId)
        {
            await using SecurityDbContext dbContext =
                await this.dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == userId);
        }

        public async ValueTask UpdateProfileImageAsync(
            Guid userId,
            byte[]? imageBytes,
            string? contentType)
        {
            await using SecurityDbContext dbContext =
                await this.dbContextFactory.CreateDbContextAsync();

            AppUser? user = await dbContext.Users
                .FirstOrDefaultAsync(user => user.Id == userId);

            if (user is null)
            {
                return;
            }

            user.ProfileImage = imageBytes;
            user.ProfileImageContentType = contentType;

            await dbContext.SaveChangesAsync();
        }
    }
}
