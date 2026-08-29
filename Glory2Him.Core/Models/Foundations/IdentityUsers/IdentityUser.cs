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

namespace Glory2Him.Core.Models.Foundations.IdentityUsers
{
    /// <summary>
    /// A read-only projection of one row in the security database's <c>AspNetUsers</c> table.
    ///
    /// <para><b>This is the host's schema, not Core's.</b> Core reads it through
    /// <c>IIdentityCoreStorageBroker</c> and owns no migrations against it — the tables belong to
    /// <c>Glory2Him.WebApp</c>'s <c>SecurityDbContext</c>, which is the only thing that may change
    /// their shape. Only the handful of columns the review-tier lookup actually needs are mapped,
    /// so a column added over there cannot break a read over here.</para>
    ///
    /// <para><b>§18.3's separation still holds.</b> The identity store stays a different database
    /// on a different connection, so there is no SQL join between a user and an approval — Core
    /// reads the two independently and combines them in memory. That is also why
    /// <c>ApprovalReviewRequest.RequestedUserDisplayName</c> stays denormalised: a name must be
    /// fixed at request time, not re-read across a boundary that may be unavailable.</para>
    /// </summary>
    public class IdentityUser
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }

        /// <summary>Whether the account is administratively disabled — an
        /// <c>AppUser</c> extension rather than a stock Identity column.</summary>
        public bool IsDisabled { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? PreferredName { get; set; }
    }
}
