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

using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.Securities
{
    /// <summary>
    /// Provides methods to apply and verify audit metadata on entities,
    /// such as created/updated timestamps and user identifiers.
    /// </summary>
    internal interface ISecurityAuditBroker
    {
        /// <summary>
        /// Ensures that audit values (e.g., created by/date) other than the ones being modified
        /// remain unchanged during modification, copying them from the stored version of the
        /// entity to the current one.
        /// </summary>
        /// <typeparam name="T">The type of the entity being verified.</typeparam>
        /// <param name="entity">The modified entity.</param>
        /// <param name="storageEntity">The original stored entity with correct creation audit values.</param>
        /// <returns>A task containing the entity with preserved other audit values.</returns>
        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity);

        /// <summary>
        /// Applies add audit values as the actor carried on an event envelope's
        /// <c>SecurityContext</c> rather than the ambient principal.
        /// </summary>
        ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, SecurityContext securityContext);

        /// <summary>
        /// Applies modify audit values as the actor carried on an event envelope's
        /// <c>SecurityContext</c>.
        /// </summary>
        ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, SecurityContext securityContext);

        /// <summary>
        /// Applies remove audit values as the actor carried on an event envelope's
        /// <c>SecurityContext</c>. A non-null <paramref name="deletionReason"/> is stamped onto
        /// the entity; a null one leaves any reason already there untouched.
        /// </summary>
        ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            SecurityContext securityContext,
            string? deletionReason = null);

        /// <summary>
        /// Resolves the acting user id from an event envelope's <c>SecurityContext</c>,
        /// consistent with the id the context-aware audit methods stamp.
        /// </summary>
        ValueTask<string> GetUserIdAsync(SecurityContext securityContext);
    }
}
