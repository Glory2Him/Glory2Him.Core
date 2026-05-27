// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Services.Foundations.Users;
using Glory2Him.Security.Client.Services.Orchestrations.Audits;

namespace Glory2Him.Security.Client.Services.Foundations.Audits
{
    internal partial class AuditOrchestrationService : IAuditOrchestrationService
    {
        private readonly IUserService userService;
        private readonly IAuditService auditService;

        public AuditOrchestrationService(IUserService userService, IAuditService auditService)
        {
            this.userService = userService;
            this.auditService = auditService;
        }

        public ValueTask<T> ApplyAddAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, claimsPrincipal, securityConfigurations);
            string userId = await this.userService.GetUserIdAsync(claimsPrincipal);

            T updatedEntity = await this.auditService
                .ApplyAddAuditValuesAsync(entity, userId, securityConfigurations);

            return updatedEntity;
        });

        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, claimsPrincipal, securityConfigurations);
            string userId = await this.userService.GetUserIdAsync(claimsPrincipal);

            T updatedEntity = await this.auditService
                .ApplyModifyAuditValuesAsync(entity, userId, securityConfigurations);

            return updatedEntity;
        });

        public ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, claimsPrincipal, securityConfigurations);
            string userId = await this.userService.GetUserIdAsync(claimsPrincipal);

            T updatedEntity = await this.auditService
                .ApplyRemoveAuditValuesAsync(entity, userId, securityConfigurations);

            return updatedEntity;
        });

        public ValueTask<T> EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);

            var updatedEntity = await this.auditService
                .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(entity, storageEntity, securityConfigurations);

            return updatedEntity;
        });

        public ValueTask<string> GetCurrentUserIdAsync(ClaimsPrincipal claimsPrincipal) =>
        TryCatch(async () =>
        {
            ValidateOnGetCurrentUserId(claimsPrincipal);

            return await this.userService.GetUserIdAsync(claimsPrincipal);
        });
    }
}
