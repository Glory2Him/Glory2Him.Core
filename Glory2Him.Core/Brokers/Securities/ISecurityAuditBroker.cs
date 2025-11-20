// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// Provides methods to apply and verify audit metadata on entities,
    /// such as created/updated timestamps and user identifiers.
    /// </summary>
    public interface ISecurityAuditBroker
    {
        /// <summary>
        /// Applies audit values related to entity creation, such as CreatedBy and CreatedDate,
        /// using the provided claims principal and security configuration.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to which audit values should be applied.</param>
        /// <returns>A task containing the audited entity.</returns>
        ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity);

        /// <summary>
        /// Applies audit values related to entity modification, such as UpdatedBy and UpdatedDate,
        /// using the provided claims principal and security configuration.
        /// </summary>
        /// <typeparam name="T">The type of the entity being audited.</typeparam>
        /// <param name="entity">The entity to which audit values should be applied.</param>
        /// <returns>A task containing the audited entity.</returns>
        ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity);
    }
}
