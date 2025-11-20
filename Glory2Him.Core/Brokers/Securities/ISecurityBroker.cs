// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Brokers.Securities
{
    public interface ISecurityBroker
    {
        ValueTask<User> GetCurrentUserAsync();
        ValueTask<bool> IsCurrentUserAuthenticatedAsync();
        ValueTask<bool> IsInRoleAsync(string roleName);
        ValueTask<bool> HasClaimTypeAsync(string claimType, string claimValue);
    }
}
