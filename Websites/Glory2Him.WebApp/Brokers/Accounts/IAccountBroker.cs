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

namespace Glory2Him.WebApp.Brokers.Accounts
{
    // Availability lookups for the registration flow, over a short-lived DbContext from a factory
    // so the live (debounced) checks never contend on the request-scoped Identity DbContext.
    public interface IAccountBroker
    {
        ValueTask<bool> UsernameExistsAsync(string userName);

        ValueTask<bool> EmailExistsAsync(string email);
    }
}
