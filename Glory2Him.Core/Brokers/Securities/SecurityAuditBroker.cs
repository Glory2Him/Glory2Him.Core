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

using System.Security.Claims;
using System.Threading.Tasks;
using G2H.Security.Client.Clients;
using G2H.Security.Client.Models.Clients;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// Provides security-related functionalities such as user authentication, claim verification, and role checks.
    /// </summary>
    internal class SecurityAuditBroker : ISecurityAuditBroker
    {
        private readonly ISecurityClient securityClient;
        private readonly SecurityConfigurations securityConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuditBroker"/> class.
        /// </summary>
        /// <param name="securityConfigurations">Contains information of the audit properties to target.</param>
        public SecurityAuditBroker(SecurityConfigurations securityConfigurations)
        {
            this.securityClient = new SecurityClient();
            this.securityConfigurations = securityConfigurations;
        }

        /// <summary>
        /// Ensures that audit values other than the ones being modified (e.g., created by/date)
        /// remain unchanged during modify operations.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity being modified.</param>
        /// <param name="storageEntity">The original stored entity used to preserve original audit values.</param>
        /// <returns>The entity with original other audit values retained.</returns>
        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity) =>
                this.securityClient.Audits
                    .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(entity, storageEntity, securityConfigurations);

        /// <summary>
        /// Applies add audit values as the actor carried on an event envelope's
        /// <see cref="SecurityContext"/> rather than the ambient principal — required for
        /// event-path processing, where dispatch (or a background retry) may run outside the
        /// original request context.
        /// </summary>
        public ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, SecurityContext securityContext) =>
            this.securityClient.Audits.ApplyAddAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations);

        /// <summary>
        /// Applies modify audit values as the actor carried on an event envelope's
        /// <see cref="SecurityContext"/>.
        /// </summary>
        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, SecurityContext securityContext) =>
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
            SecurityContext securityContext,
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
        public async ValueTask<string> GetUserIdAsync(SecurityContext securityContext) =>
            await securityClient.Audits.GetUserIdAsync(CreateClaimsPrincipal(securityContext));

        // Shared with AccessBroker, which resolves the actor these audit values are later
        // compared against. See SecurityContextPrincipalFactory for why there is only one.
        private static ClaimsPrincipal CreateClaimsPrincipal(SecurityContext securityContext) =>
            SecurityContextPrincipalFactory.Create(securityContext);
    }
}
