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

using G2H.Security.Client.Clients.Access;
using G2H.Security.Client.Clients.Audits;
using G2H.Security.Client.Clients.Users;

namespace G2H.Security.Client.Clients
{
    /// <summary>
    /// The security surface, grouped by the question being asked. One client, one configuration,
    /// and one place a caller's rights are decided.
    /// </summary>
    public interface ISecurityClient
    {
        /// <summary>
        /// Who the caller is — identity, authentication state, roles and claims, read from a
        /// <see cref="System.Security.Claims.ClaimsPrincipal"/>.
        /// </summary>
        IUserClient Users { get; }

        /// <summary>
        /// Who did what and when — stamps the created, updated and deleted audit values on an
        /// entity, and holds them unchanged when a caller must not move them.
        /// </summary>
        IAuditClient Audits { get; }

        /// <summary>
        /// Whether the caller may do it — approval policy decisions. Grouped here beside identity
        /// because "may they?" is one question and should have one place to ask it; a separate
        /// approvals client would have duplicated the claims plumbing and given the system two
        /// answers.
        /// </summary>
        IAccessClient Access { get; }
    }
}
