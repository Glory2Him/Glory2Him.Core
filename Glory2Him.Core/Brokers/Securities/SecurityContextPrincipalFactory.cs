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
using System.Security.Claims;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// Rebuilds a <see cref="ClaimsPrincipal"/> from an event envelope's normalized actor, so the
    /// security client's pipeline — which resolves a user id from <c>oid</c> / <c>nameidentifier</c>
    /// claims — sees the ORIGINAL caller regardless of what identity the current process runs under.
    ///
    /// <para><b>There is exactly one of these, and that is the point.</b> Two brokers need it:
    /// <see cref="SecurityAuditBroker"/> to stamp <c>CreatedBy</c>, and <see cref="AccessBroker"/>
    /// to resolve the actor that the self-review and self-approval bars compare <i>against</i> that
    /// same <c>CreatedBy</c>. Those comparisons are only meaningful while both sides are built the
    /// same way. A second copy of this method would not fail loudly — it would quietly answer
    /// "not the author" for the author, which is the permissive direction.</para>
    /// </summary>
    internal static class SecurityContextPrincipalFactory
    {
        public static ClaimsPrincipal Create(SecurityContext securityContext)
        {
            var claims = new List<Claim>();

            if (string.IsNullOrWhiteSpace(securityContext?.SubjectId) is false)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, securityContext!.SubjectId!));

            if (string.IsNullOrWhiteSpace(securityContext?.Username) is false)
                claims.Add(new Claim(ClaimTypes.Name, securityContext!.Username!));

            foreach (string role in securityContext?.Roles ?? [])
                claims.Add(new Claim(ClaimTypes.Role, role));

            ClaimsIdentity identity = securityContext?.IsAuthenticated == true
                ? new ClaimsIdentity(claims, authenticationType: "EventEnvelope")
                : new ClaimsIdentity(claims);

            return new ClaimsPrincipal(identity);
        }
    }
}
