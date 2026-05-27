// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Threading.Tasks;
using Glory2Him.Security.Client.Models.Clients;

namespace Glory2Him.Security.Client.Services.Foundations.Audits
{
    internal interface IAuditService
    {
        ValueTask<T> ApplyAddAuditValuesAsync<T>(T entity, string userId, SecurityConfigurations securityConfigurations);
        ValueTask<T> ApplyModifyAuditValuesAsync<T>(T entity, string userId, SecurityConfigurations securityConfigurations);
        ValueTask<T> ApplyRemoveAuditValuesAsync<T>(T entity, string userId, SecurityConfigurations securityConfigurations);

        ValueTask<T> EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations);
    }
}
