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
using G2H.Security.Client.Models.Clients;

namespace G2H.Security.Client.Services.Foundations.Audits
{
    internal interface IAuditService
    {
        ValueTask<T> ApplyAddAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations);

        ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations);

        ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations,
            string? deletionReason = null);

        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations);

        ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations);
    }
}
