// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Clients.Audits.Exceptions;
using Glory2Him.Security.Client.Models.Orchestrations.Audits.Exceptions;
using Glory2Him.Security.Client.Services.Orchestrations.Audits;
using Xeptions;

namespace Glory2Him.Security.Client.Clients.Audits
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
