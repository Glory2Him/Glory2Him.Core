// ---
// skill: the-standard-brokers
// type: template
// source-section: "1. Brokers"
// ---

// ISecurityAuditBroker.cs — interface
using System.Threading.Tasks;
using {Namespace}.Models.Events;

namespace {Namespace}.Brokers.Securities
{
    /// <summary>
    /// Provides methods to apply and verify audit metadata on entities,
    /// such as created/updated timestamps and user identifiers.
    /// </summary>
    public interface ISecurityAuditBroker
    {
        /// <summary>
        /// Applies audit values related to entity creation, such as CreatedBy and CreatedDate,
        /// using the actor carried on an event envelope's <see cref="SecurityContext"/> rather
        /// than an ambient principal — required for event-path processing, where dispatch (or a
        /// background retry) may run outside the original request context.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to which audit values should be applied.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <returns>A task containing the audited entity.</returns>
        ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, SecurityContext securityContext);

        /// <summary>
        /// Applies audit values related to entity modification, such as UpdatedBy and UpdatedDate,
        /// using the actor carried on an event envelope's <see cref="SecurityContext"/>.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to which audit values should be applied.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <returns>A task containing the audited entity.</returns>
        ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, SecurityContext securityContext);

        /// <summary>
        /// Applies audit values related to logical deletion, such as UpdatedBy and UpdatedDate,
        /// using the actor carried on an event envelope's <see cref="SecurityContext"/>.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to which deletion audit values should be applied.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <param name="deletionReason">
        /// The reason the entity is being removed, stamped alongside the other deletion audit
        /// values. When null, any reason already on the entity is left untouched.
        /// </param>
        /// <returns>A task containing the entity with deletion audit values.</returns>
        ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            SecurityContext securityContext,
            string? deletionReason = null);

        /// <summary>
        /// Ensures that audit values related to entity creation remain unchanged during modification,
        /// copying them from the stored version of the entity to the current one.
        /// </summary>
        /// <typeparam name="T">The type of the entity being verified.</typeparam>
        /// <param name="entity">The modified entity.</param>
        /// <param name="storageEntity">The original stored entity with correct creation audit values.</param>
        /// <returns>A task containing the entity with preserved creation audit values.</returns>
        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity);

        /// <summary>
        /// Resolves the acting user id from an event envelope's <see cref="SecurityContext"/>,
        /// consistent with the id the context-aware audit methods stamp.
        /// </summary>
        /// <param name="securityContext">The actor to resolve the user id for.</param>
        ValueTask<string> GetUserIdAsync(SecurityContext securityContext);
    }
}

// SecurityAuditBroker.cs — implementation
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using {Namespace}.Models.Events;
using {Namespace}.Security.Client.Clients;
using {Namespace}.Security.Client.Models.Clients;

namespace {Namespace}.Brokers.Securities
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
        /// Applies auditing metadata for an add operation to the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <returns>The audited entity with add metadata applied.</returns>
        public ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, SecurityContext securityContext) =>
            this.securityClient.Audits.ApplyAddAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations);

        /// <summary>
        /// Applies auditing metadata for a modify operation to the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <returns>The audited entity with modify metadata applied.</returns>
        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, SecurityContext securityContext) =>
            this.securityClient.Audits.ApplyModifyAuditValuesAsync(
                entity,
                CreateClaimsPrincipal(securityContext),
                securityConfigurations);

        /// <summary>
        /// Applies auditing metadata for a remove (soft delete) operation to the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to audit for removal.</param>
        /// <param name="securityContext">The actor to stamp the audit values with.</param>
        /// <param name="deletionReason">The reason for the removal, or null to leave it unchanged.</param>
        /// <returns>The audited entity with remove metadata applied.</returns>
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
        /// Resolves the acting user id from an event envelope's <see cref="SecurityContext"/>.
        /// </summary>
        /// <param name="securityContext">The actor to resolve the user id for.</param>
        public async ValueTask<string> GetUserIdAsync(SecurityContext securityContext) =>
            await this.securityClient.Audits.GetUserIdAsync(CreateClaimsPrincipal(securityContext));

        // Rebuilds a principal from the context's normalized actor so the same security client
        // pipeline (which resolves the user id from oid/nameidentifier claims) stamps the
        // ORIGINAL caller, regardless of what identity the current process runs under.
        private static ClaimsPrincipal CreateClaimsPrincipal(SecurityContext securityContext)
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
