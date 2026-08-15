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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.Security.Client.Clients;
using G2H.Security.Client.Models.Clients;
using Microsoft.AspNetCore.Http;

namespace G2H.EventEnvelope.Client.Brokers.Securities
{
    /// <summary>
    /// Provides security-related functionalities such as user authentication, claim verification, and role checks.
    /// Supports both REST API (using <see cref="IHttpContextAccessor"/>) and Azure Functions (using access token).
    /// </summary>
    internal class SecurityAuditBroker : ISecurityAuditBroker
    {
        private readonly ClaimsPrincipal claimsPrincipal;
        private readonly ISecurityClient securityClient;
        private readonly SecurityConfigurations securityConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuditBroker"/> class 
        /// using <see cref="IHttpContextAccessor"/>.
        /// This constructor is intended for REST API usage.
        /// </summary>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
        public SecurityAuditBroker(
            IHttpContextAccessor httpContextAccessor,
            SecurityConfigurations securityConfigurations)
        {
            claimsPrincipal = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
            this.securityClient = new SecurityClient();
            this.securityConfigurations = securityConfigurations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuditBroker"/> class using an access token.
        /// This constructor is intended for Azure Function / non REST API usage.
        /// </summary>
        /// <param name="accessToken">A JWT access token containing user claims.</param>
        /// <param name="securityConfigurations">Contains information of the audit properties to target.</param>
        public SecurityAuditBroker(string accessToken, SecurityConfigurations securityConfigurations)
        {
            this.claimsPrincipal = GetClaimsPrincipalFromToken(accessToken);
            this.securityClient = new SecurityClient();
            this.securityConfigurations = securityConfigurations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuditBroker"/> 
        /// class using a <see cref="ClaimsPrincipal"/>.
        /// This constructor is intended for Azure Functions or non-REST API usage.
        /// </summary>
        /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> containing user claims.</param>
        /// <param name="securityConfigurations">Contains information of the audit properties to target.</param>
        public SecurityAuditBroker(ClaimsPrincipal claimsPrincipal, SecurityConfigurations securityConfigurations)
        {
            this.claimsPrincipal = claimsPrincipal;
            this.securityConfigurations = securityConfigurations;
            this.securityClient = new SecurityClient();
        }

        /// <summary>
        /// Extracts a <see cref="ClaimsPrincipal"/> from a given JWT token.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing claims from the token.</returns>
        private static ClaimsPrincipal GetClaimsPrincipalFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");

            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Applies auditing metadata for an add operation to the specified entity.
        /// Sets created and updated audit fields based on the current user.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit.</param>
        /// <returns>The audited entity with add metadata applied.</returns>
        public ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity) =>
            this.securityClient.Audits.ApplyAddAuditValuesAsync(entity, claimsPrincipal, securityConfigurations);

        /// <summary>
        /// Applies auditing metadata for a modify operation to the specified entity.
        /// Sets updated audit fields based on the current user.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit.</param>
        /// <returns>The audited entity with modify metadata applied.</returns>
        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity) =>
                this.securityClient.Audits.ApplyModifyAuditValuesAsync(entity, claimsPrincipal, securityConfigurations);

        /// <summary>
        /// Applies auditing metadata for a remove (soft delete) operation to the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit for removal.</param>
        /// <param name="deletionReason">The reason for the removal, or null to leave it unchanged.</param>
        /// <returns>The audited entity with remove metadata applied.</returns>
        public ValueTask<T> ApplyRemoveAuditValuesAsync<T>(T entity, string? deletionReason = null) =>
                this.securityClient.Audits.ApplyRemoveAuditValuesAsync(
                    entity,
                    claimsPrincipal,
                    securityConfigurations,
                    deletionReason);

        /// <summary>
        /// Ensures that add audit values (e.g., created by/date) remain unchanged during modify operations.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity being modified.</param>
        /// <param name="storageEntity">The original stored entity used to preserve original audit values.</param>
        /// <returns>The entity with original add audit values retained.</returns>
        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity) =>
                this.securityClient.Audits
                    .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(entity, storageEntity, securityConfigurations);

        /// <summary>
        /// Retrieves the user identifier from the given claims principal.
        /// </summary>
        /// <param name="claimsPrincipal">The user context containing claims.</param>
        /// <returns>The user identifier string.</returns>
        /// <remarks>
        /// If no valid user identifier is found, a fallback (such as <c>"Anonymous"</c>) may be returned.
        /// </remarks>
        /// <example>
        /// <code>
        /// string userId = await auditClient.GetUserIdAsync(User);
        /// // e.g. "Alice" or "Anonymous"
        /// </code>
        /// </example>
        public async ValueTask<string> GetUserIdAsync() =>
            await securityClient.Audits.GetUserIdAsync(claimsPrincipal);

        /// <summary>
        /// Applies add audit values as the actor carried on an event envelope's
        /// <see cref="SecurityContext"/> rather than the ambient principal — required for
        /// event-path processing, where dispatch (or a background retry) may run outside the
        /// original request context.
        /// </summary>
        public ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, EventSecurityContext securityContext) =>
            this.securityClient.Audits.ApplyAddAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations);

        /// <summary>
        /// Applies modify audit values as the actor carried on an event envelope's
        /// <see cref="SecurityContext"/>.
        /// </summary>
        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, EventSecurityContext securityContext) =>
            this.securityClient.Audits.ApplyModifyAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations);

        /// <summary>
        /// Applies remove audit values as the actor carried on an event envelope's
        /// <see cref="SecurityContext"/>.
        /// </summary>
        public ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            EventSecurityContext securityContext,
            string? deletionReason = null) =>
            this.securityClient.Audits.ApplyRemoveAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations,
                deletionReason);

        /// <summary>
        /// Resolves the acting user id from an event envelope's <see cref="SecurityContext"/>,
        /// consistent with the id the context-aware audit methods stamp.
        /// </summary>
        public async ValueTask<string> GetUserIdAsync(EventSecurityContext securityContext) =>
            await securityClient.Audits.GetUserIdAsync(CreateClaimsPrincipal(securityContext));

        // Rebuilds a principal from the envelope's normalized actor so the same security
        // client pipeline (which resolves the user id from oid/nameidentifier claims) stamps
        // the ORIGINAL caller, regardless of what identity the current process runs under.
        private static ClaimsPrincipal CreateClaimsPrincipal(EventSecurityContext securityContext)
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
