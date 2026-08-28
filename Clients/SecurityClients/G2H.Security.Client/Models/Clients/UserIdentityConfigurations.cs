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

namespace G2H.Security.Client.Models.Clients
{
    /// <summary>
    /// Tells the client which claim carries the user's identity. Supplied once, optionally, when
    /// the <c>SecurityClient</c> is constructed.
    /// </summary>
    /// <remarks>
    /// <para>The value resolved here becomes <c>CreatedBy</c>, <c>UpdatedBy</c> and
    /// <c>DeletedBy</c> on every audited entity, and is what an ownership check compares against.
    /// It must therefore be a STABLE ACCOUNT IDENTIFIER, never a display name — two accounts can
    /// share a name, and an owner check that matches on one is a privilege escalation.</para>
    ///
    /// <para><b>The default is ASP.NET Core Identity's.</b>
    /// <see cref="ClaimTypes.NameIdentifier"/> is where Identity puts the user's primary key
    /// (<c>IdentityOptions.ClaimsIdentity.UserIdClaimType</c>), so a host using Identity — with or
    /// without external login providers, which still issue an application cookie built from the
    /// local user record — needs no configuration at all.</para>
    ///
    /// <para>A host on a different identity provider overrides it. Entra ID, for instance, carries
    /// the object id in <c>oid</c>:</para>
    ///
    /// <code>
    /// var securityClient = new SecurityClient(new UserIdentityConfigurations
    /// {
    ///     UserIdClaimTypes = new[]
    ///     {
    ///         "oid",
    ///         "http://schemas.microsoft.com/identity/claims/objectidentifier"
    ///     }
    /// });
    /// </code>
    /// </remarks>
    public class UserIdentityConfigurations
    {
        /// <summary>
        /// The claim types to read the user id from, in order — the first one present on the
        /// principal wins. Ordered rather than a single value so a host federating more than one
        /// provider can list each provider's claim and let precedence decide.
        /// </summary>
        public IReadOnlyList<string> UserIdClaimTypes { get; set; } =
            new[] { ClaimTypes.NameIdentifier };
    }
}
