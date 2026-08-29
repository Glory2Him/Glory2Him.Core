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

namespace Glory2Him.Core.Services.Foundations.IdentityUsers
{
    /// <summary>
    /// The read-only foundation over the identity store (§12.7.1). It answers exactly one kind of
    /// question — WHO holds a given set of role names — because that is the only thing the
    /// approval workflow needs from the security database and every extra member would widen a
    /// cross-component read surface for no caller.
    ///
    /// <para><b>It applies no policy.</b> Composing which role names constitute the review tier
    /// for an entity is §18.6's rule and belongs to the caller that knows the entity type; this
    /// service is handed the names and returns the members. Keeping the composition out of here
    /// is what stops the tier convention having two homes.</para>
    ///
    /// <para><b>Internal on purpose.</b> Enumerating users is a privileged surface (§16.7.4), and
    /// nothing outside Core should be able to reach it without going through an orchestration
    /// that gates the caller first.</para>
    /// </summary>
    internal interface IIdentityUserService
    {
        /// <summary>
        /// The active accounts holding ANY of <paramref name="roleNames"/>, matched
        /// case-insensitively against the role name.
        ///
        /// <para>Disabled accounts are excluded: inviting somebody who cannot sign in produces an
        /// invitation nobody can answer, and the row would sit in the panel forever. An empty or
        /// null set of names returns no users rather than everybody — the fail-closed reading, so
        /// a caller that composed the tier wrongly invites nobody instead of the whole
        /// directory.</para>
        /// </summary>
        ValueTask<IReadOnlyList<IdentityUser>> RetrieveIdentityUsersInRolesAsync(
            IEnumerable<string> roleNames,
            CancellationToken cancellationToken = default);
    }
}
