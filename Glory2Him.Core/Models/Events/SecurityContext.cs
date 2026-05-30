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

using System.Collections.Generic;

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// A normalized, transport-agnostic representation of the authenticated caller.
    /// Built once at the application entry point from the incoming <c>ClaimsPrincipal</c> and
    /// carried through the event envelope so that orchestration services and event handlers
    /// never depend directly on <c>HttpContext</c>, <c>IHttpContextAccessor</c>, or raw JWT tokens.
    /// </summary>
    public sealed class SecurityContext
    {
        /// <summary>
        /// The unique subject identifier of the authenticated caller, mapped from the OAuth 2.0 / OpenID Connect
        /// <c>sub</c> claim. Null for machine-to-machine flows where no human subject is present.
        /// </summary>
        public string? SubjectId { get; init; }

        /// <summary>
        /// The display name or login name of the authenticated user. Null for machine-to-machine flows.
        /// </summary>
        public string? Username { get; init; }

        /// <summary>
        /// The tenant the caller belongs to, when multi-tenancy is in use. Null for single-tenant deployments.
        /// </summary>
        public string? TenantId { get; init; }

        /// <summary>
        /// The roles assigned to the authenticated caller, used for role-based authorization decisions.
        /// </summary>
        public IReadOnlyList<string> Roles { get; init; }

        /// <summary>
        /// The OAuth 2.0 scopes granted to the caller, used for scope-based API access control.
        /// </summary>
        public IReadOnlyList<string> Scopes { get; init; }

        /// <summary>
        /// Fine-grained permissions granted to the caller beyond role and scope level.
        /// </summary>
        public IReadOnlyList<string> Permissions { get; init; }

        /// <summary>
        /// Indicates whether the caller has been successfully authenticated.
        /// </summary>
        public bool IsAuthenticated { get; init; }

        /// <summary>
        /// Identifies the mechanism by which the caller was authenticated.
        /// </summary>
        public AuthenticationType AuthenticationType { get; init; }

        /// <summary>
        /// The OAuth 2.0 client identifier of the application making the request.
        /// </summary>
        public string? ClientId { get; init; }

        /// <summary>
        /// The human-readable name of the client application making the request.
        /// </summary>
        public string? ClientApplicationName { get; init; }

        /// <summary>
        /// Indicates whether the caller is a system identity such as an internal service or background worker
        /// rather than a human user or external client.
        /// </summary>
        public bool IsSystemIdentity { get; init; }

        /// <summary>
        /// The subject identifier of the principal who delegated access to the current caller.
        /// Populated only when <see cref="AuthenticationType"/> is <see cref="AuthenticationType.Delegated"/>.
        /// </summary>
        public string? DelegatedBySubjectId { get; init; }
    }
}
