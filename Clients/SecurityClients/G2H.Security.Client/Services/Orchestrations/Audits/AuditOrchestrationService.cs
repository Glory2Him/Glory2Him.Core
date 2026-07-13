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

using System.Security.Claims;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Services.Foundations.Users;
using G2H.Security.Client.Services.Orchestrations.Audits;

namespace G2H.Security.Client.Services.Foundations.Audits
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
            SecurityConfigurations securityConfigurations,
            string? deletionReason = null) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, claimsPrincipal, securityConfigurations);
            string userId = await this.userService.GetUserIdAsync(claimsPrincipal);

            T updatedEntity = await this.auditService
                .ApplyRemoveAuditValuesAsync(entity, userId, securityConfigurations, deletionReason);

            return updatedEntity;
        });

        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);

            var updatedEntity = await this.auditService
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(entity, storageEntity, securityConfigurations);

            return updatedEntity;
        });

        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);

            var updatedEntity = await this.auditService
                .EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync<T>(entity, storageEntity, securityConfigurations);

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
