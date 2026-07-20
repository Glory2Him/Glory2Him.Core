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
using G2H.Security.Client.Models.Clients;

namespace G2H.Security.Client.Clients.Audits
{
    /// <summary>
    /// Provides methods for applying and validating audit values 
    /// (e.g., CreatedBy, UpdatedBy, DeletedBy, and related timestamps)
    /// on entities in accordance with security configurations.
    /// </summary>
    public interface IAuditClient
    {
        /// <summary>
        /// Applies audit values when a new entity is being added.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to apply audit values to.</param>
        /// <param name="claimsPrincipal">The user context used to determine who is performing the operation.</param>
        /// <param name="securityConfigurations">The security configuration settings for audit enforcement.</param>
        /// <returns>The entity with applied audit values.</returns>
        /// <remarks>
        /// Typically sets:
        /// <list type="bullet">
        ///   <item><c>CreatedBy</c> → current user ID</item>
        ///   <item><c>CreatedDate</c> → current UTC timestamp</item>
        ///   <item><c>UpdatedBy</c> and <c>UpdatedDate</c> may be set to the same values initially</item>
        /// </list>
        /// <example>
        /// For a new record added by user "Alice":
        /// <code>
        /// entity.CreatedBy = "Alice";
        /// entity.CreatedDate = 2025-09-05T17:00:00Z;
        /// entity.UpdatedBy = "Alice";
        /// entity.UpdatedDate = 2025-09-05T17:00:00Z;
        /// </code>
        /// </example>
        /// </remarks>
        ValueTask<T> ApplyAddAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations);

        /// <summary>
        /// Applies audit values when an existing entity is being modified.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to apply audit values to.</param>
        /// <param name="claimsPrincipal">The user context used to determine who is performing the operation.</param>
        /// <param name="securityConfigurations">The security configuration settings for audit enforcement.</param>
        /// <returns>The entity with updated audit values.</returns>
        /// <remarks>
        /// Typically sets:
        /// <list type="bullet">
        ///   <item><c>UpdatedBy</c> → current user ID</item>
        ///   <item><c>UpdatedDate</c> → current UTC timestamp</item>
        /// </list>
        /// <example>
        /// If user "Bob" modifies an existing record:
        /// <code>
        /// entity.UpdatedBy = "Bob";
        /// entity.UpdatedDate = 2025-09-05T17:15:00Z;
        /// </code>
        /// </example>
        /// </remarks>
        ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations);

        /// <summary>
        /// Ensures that add/delete audit values remain unchanged during modification.
        /// </summary>
        /// <typeparam name="T">The type of the entity being validated.</typeparam>
        /// <param name="entity">The modified entity.</param>
        /// <param name="storageEntity">The original stored entity for comparison.</param>
        /// <param name="securityConfigurations">The security configuration settings for audit enforcement.</param>
        /// <returns>The entity with validated audit values.</returns>
        /// <remarks>
        /// This method prevents overwriting of immutable audit fields:
        /// <list type="bullet">
        ///   <item><c>CreatedBy</c></item>
        ///   <item><c>CreatedDate</c></item>
        /// </list>
        /// <example>
        /// If a malicious user tries to change <c>CreatedBy</c> from "Alice" to "Bob",
        /// this method will restore the original "Alice" value from the stored entity.
        /// </example>
        /// </remarks>
        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations);

        /// <summary>
        /// Ensures that add/update audit values remain unchanged during removal.
        /// </summary>
        /// <typeparam name="T">The type of the entity being validated.</typeparam>
        /// <param name="entity">The entity being removed.</param>
        /// <param name="storageEntity">The original stored entity for comparison.</param>
        /// <param name="securityConfigurations">The security configuration settings for audit enforcement.</param>
        /// <returns>The entity with validated audit values.</returns>
        /// <remarks>
        /// This method prevents overwriting of immutable audit fields:
        /// <list type="bullet">
        ///   <item><c>CreatedBy</c></item>
        ///   <item><c>CreatedDate</c></item>
        ///   <item><c>UpdatedBy</c></item>
        ///   <item><c>UpdatedDate</c></item>
        /// </list>
        /// <example>
        /// If a malicious user tries to change <c>CreatedBy</c> from "Alice" to "Bob",
        /// this method will restore the original "Alice" value from the stored entity.
        /// </example>
        /// </remarks>
        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations);

        /// <summary>
        /// Applies audit values when an entity is being removed (soft-delete OR where temporal tables needs the history).
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to apply audit values to.</param>
        /// <param name="claimsPrincipal">The user context used to determine who is performing the operation.</param>
        /// <param name="securityConfigurations">The security configuration settings for audit enforcement.</param>
        /// <returns>The entity with applied removal audit values.</returns>
        /// <remarks>
        /// Typically sets:
        /// <list type="bullet">
        ///   <item><c>DeletedBy</c> → current user ID</item>
        ///   <item><c>DeletedDate</c> → current UTC timestamp</item>
        /// </list>
        /// <example>
        /// If user "Charlie" soft-deletes a record:
        /// <code>
        /// entity.DeletedBy = "Charlie";
        /// entity.DeletedDate = 2025-09-05T17:30:00Z;
        /// </code>
        /// </example>
        /// </remarks>
        ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations,
            string? deletionReason = null);

        /// <summary>
        /// Retrieves the current user identifier from the given claims principal.
        /// </summary>
        /// <param name="claimsPrincipal">The user context containing claims.</param>
        /// <returns>The user identifier string.</returns>
        /// <remarks>
        /// If no valid user identifier is found, a fallback (such as <c>"Anonymous"</c>) may be returned.
        /// </remarks>
        /// <example>
        /// <code>
        /// string userId = await auditClient.GetUserIdAsync(claimsPrincipal);
        /// // e.g. "Alice" or "Anonymous"
        /// </code>
        /// </example>
        ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal);
    }
}
