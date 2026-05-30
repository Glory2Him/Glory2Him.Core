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

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Clients.Audits.Exceptions;
using G2H.Security.Client.Models.Orchestrations.Audits.Exceptions;
using G2H.Security.Client.Services.Orchestrations.Audits;
using Xeptions;

namespace G2H.Security.Client.Clients.Audits
{
    internal class AuditClient : IAuditClient
    {
        private readonly IAuditOrchestrationService auditOrchestrationService;

        public AuditClient(IAuditOrchestrationService auditOrchestrationService)
        {
            this.auditOrchestrationService = auditOrchestrationService;
        }

        public async ValueTask<T> ApplyAddAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations)
        {
            try
            {
                return await this.auditOrchestrationService
                    .ApplyAddAuditValuesAsync<T>(entity, claimsPrincipal, securityConfigurations);
            }
            catch (AuditOrchestrationValidationException auditOrchestrationValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyValidationException auditOrchestrationDependencyValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationDependencyValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyException auditOrchestrationDependencyException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationDependencyException.InnerException as Xeption);
            }
            catch (AuditOrchestrationServiceException auditOrchestrationServiceException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAuditClientServiceException(exception);
            }
        }

        public async ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations)
        {
            try
            {
                return await this.auditOrchestrationService
                    .ApplyModifyAuditValuesAsync(entity, claimsPrincipal, securityConfigurations);
            }
            catch (AuditOrchestrationValidationException auditOrchestrationValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyValidationException auditOrchestrationDependencyValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationDependencyValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyException auditOrchestrationDependencyException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationDependencyException.InnerException as Xeption);
            }
            catch (AuditOrchestrationServiceException auditOrchestrationServiceException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAuditClientServiceException(exception);
            }
        }

        public async ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            ClaimsPrincipal claimsPrincipal,
            SecurityConfigurations securityConfigurations)
        {
            try
            {
                return await this.auditOrchestrationService
                    .ApplyRemoveAuditValuesAsync(entity, claimsPrincipal, securityConfigurations);
            }
            catch (AuditOrchestrationValidationException auditOrchestrationValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyValidationException auditOrchestrationDependencyValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationDependencyValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyException auditOrchestrationDependencyException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationDependencyException.InnerException as Xeption);
            }
            catch (AuditOrchestrationServiceException auditOrchestrationServiceException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAuditClientServiceException(exception);
            }
        }

        public async ValueTask<T> EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations)
        {
            try
            {
                return await this.auditOrchestrationService
                    .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(entity, storageEntity, securityConfigurations);
            }
            catch (AuditOrchestrationValidationException auditOrchestrationValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyValidationException auditOrchestrationDependencyValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationDependencyValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyException auditOrchestrationDependencyException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationDependencyException.InnerException as Xeption);
            }
            catch (AuditOrchestrationServiceException auditOrchestrationServiceException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAuditClientServiceException(exception);
            }
        }

        public async ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                return await this.auditOrchestrationService.GetCurrentUserIdAsync(claimsPrincipal);
            }
            catch (AuditOrchestrationValidationException auditOrchestrationValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyValidationException auditOrchestrationDependencyValidationException)
            {
                throw CreateAuditClientValidationException(
                    auditOrchestrationDependencyValidationException.InnerException as Xeption);
            }
            catch (AuditOrchestrationDependencyException auditOrchestrationDependencyException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationDependencyException.InnerException as Xeption);
            }
            catch (AuditOrchestrationServiceException auditOrchestrationServiceException)
            {
                throw CreateAuditClientDependencyException(
                    auditOrchestrationServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAuditClientServiceException(exception);
            }
        }

        private static AuditClientValidationException CreateAuditClientValidationException(Xeption innerException)
        {
            return new AuditClientValidationException(
                message: "Audit client validation error occurred, fix the error and try again.",
                innerException,
                data: innerException.Data);
        }

        private static AuditClientDependencyException CreateAuditClientDependencyException(Xeption innerException)
        {
            return new AuditClientDependencyException(
                message: "Audit client dependency error occurred, please contact support.",
                innerException,
                data: innerException.Data);
        }

        private static AuditClientServiceException CreateAuditClientServiceException(Exception innerException)
        {
            return new AuditClientServiceException(
                message: "Audit client service error occurred, please contact support.",
                innerException,
                data: innerException.Data);
        }

    }
}
