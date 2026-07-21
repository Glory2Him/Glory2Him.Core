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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using G2H.StorageClient.Tests.Integrations.Models.Users;

namespace G2H.StorageClient.Tests.Integrations.Brokers.Storages
{
    public partial interface IStorageBroker
    {
        ValueTask<User> InsertUserAsync(User user);
        ValueTask<IQueryable<User>> SelectAllUsersAsync();
        ValueTask<User> SelectUserByIdAsync(Guid userId);
        ValueTask<User> UpdateUserAsync(User user);
        ValueTask<User> DeleteUserAsync(User user);
        ValueTask BulkInsertUsersAsync(IEnumerable<User> users);
        ValueTask<IEnumerable<User>> BulkReadUsersAsync(IEnumerable<User> users);
        ValueTask BulkUpdateUsersAsync(IEnumerable<User> users);
        ValueTask BulkDeleteUsersAsync(IEnumerable<User> users);
        ValueTask BulkUpsertUsersAsync(IEnumerable<User> users);
        ValueTask<bool> UserExistsAsync(Guid userId);
    }
}
