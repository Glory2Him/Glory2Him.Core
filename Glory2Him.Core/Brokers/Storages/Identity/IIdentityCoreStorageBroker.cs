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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.IdentityUsers;

namespace Glory2Him.Core.Brokers.Storages.Identity
{
    /// <summary>
    /// Core's read window onto the SECURITY database — the ASP.NET Identity store behind the
    /// <c>Glory2HimSecurityConnection</c> connection string, which is a different database from
    /// the one <see cref="Sql.IStorageBroker"/> serves.
    ///
    /// <para><b>Why Core needs it at all.</b> §7.9 rule 3 refuses an invitation unless the invited
    /// person satisfies the review tier, and §16.7.4's candidates read has to enumerate the people
    /// who do. Both are questions about ROLE MEMBERSHIP, and role membership lives in the identity
    /// store. <c>ISecurityClient.Users</c> cannot answer either: it reads a
    /// <c>ClaimsPrincipal</c>, so it only ever describes the CURRENT caller. Without this broker
    /// the foundation would have to take the host's word for who is eligible — the same
    /// caller-supplied-identity mistake that <c>ApprovalReview.ReviewerId</c> was deleted for,
    /// where free text let one reviewer meet a three-approval threshold alone.</para>
    ///
    /// <para><b>READ-ONLY, and the interface is the enforcement.</b> There is no Insert, Update,
    /// Delete or Bulk* member here and there must never be one: the identity store is another
    /// component's source of truth, and Core writing to it would put two owners on one schema.
    /// The account this connection string authenticates as should be granted SELECT and nothing
    /// more, so the rule survives a future edit to this file.</para>
    ///
    /// <para><b>Core owns no migrations against these tables.</b> The shape belongs to
    /// <c>Glory2Him.WebApp</c>'s <c>SecurityDbContext</c>. Because the solution now has two
    /// DbContexts, EF tooling needs telling which one it is working on — see the note on
    /// <see cref="IdentityCoreStorageBroker"/>.</para>
    /// </summary>
    internal interface IIdentityCoreStorageBroker
    {
        /// <summary>
        /// The active accounts holding ANY of the given role names, matched on the upper-cased
        /// role name so the read does not depend on the host normalizer being the stock one.
        ///
        /// <para>ONE purpose-built read rather than three table queryables, and that is
        /// deliberate. The join belongs here because EF owns it - materialising it in a service
        /// would either drag EF async into the service layer or pull three whole tables into
        /// memory to join them there. What does NOT belong here is which names constitute the
        /// tier: that is 18.6 and it is composed by the orchestration, so this is handed finished
        /// names and only reports membership.</para>
        ///
        /// <para>Disabled accounts are excluded: an invitation nobody can sign in to answer would
        /// sit in the panel forever.</para>
        /// </summary>
        ValueTask<List<IdentityUser>> SelectIdentityUsersInRolesAsync(
            IReadOnlyList<string> normalizedRoleNames,
            CancellationToken cancellationToken = default);
    }
}
