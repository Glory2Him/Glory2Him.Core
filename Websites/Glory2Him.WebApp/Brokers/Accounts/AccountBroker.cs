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

using System.Threading.Tasks;
using Glory2Him.WebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Brokers.Accounts
{
    public sealed class AccountBroker : IAccountBroker
    {
        private readonly IDbContextFactory<SecurityDbContext> dbContextFactory;

        public AccountBroker(IDbContextFactory<SecurityDbContext> dbContextFactory) =>
            this.dbContextFactory = dbContextFactory;

        public async ValueTask<bool> UsernameExistsAsync(string userName)
        {
            // Identity's default normalizer upper-cases; match on the normalized column.
            string normalized = userName.ToUpperInvariant();

            await using SecurityDbContext dbContext =
                await this.dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.NormalizedUserName == normalized);
        }

        public async ValueTask<bool> EmailExistsAsync(string email)
        {
            string normalized = email.ToUpperInvariant();

            await using SecurityDbContext dbContext =
                await this.dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.NormalizedEmail == normalized);
        }
    }
}
