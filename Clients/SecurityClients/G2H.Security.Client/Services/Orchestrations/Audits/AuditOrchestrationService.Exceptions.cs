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

using System;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Audits.Exceptions;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;
using G2H.Security.Client.Models.Orchestrations.Audits.Exceptions;
using Xeptions;

namespace G2H.Security.Client.Services.Foundations.Audits
{
    internal partial class AuditOrchestrationService
    {
        private delegate ValueTask<T> ReturningObjectFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningObjectFunction<T> returningObjectFunction)
        {
            try
            {
                return await returningObjectFunction();
            }
            catch (InvalidArgumentAuditOrchestrationException invalidArgumentAuditOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(invalidArgumentAuditOrchestrationException);
            }
            catch (UserValidationException userValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(userValidationException);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(userDependencyValidationException);
            }
            catch (AuditValidationException auditValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(auditValidationException);
            }
            catch (AuditDependencyValidationException auditDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(auditDependencyValidationException);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(userDependencyException);
            }
            catch (AuditDependencyException auditDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(auditDependencyException);
            }
            catch (UserServiceException userServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(userServiceException);
            }
            catch (AuditServiceException auditServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(auditServiceException);
            }
            catch (Exception exception)
            {
                var failedAuditOrchestrationServiceException =
                    new FailedAuditOrchestrationServiceException(
                        message: "Failed audit orchestration service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedAuditOrchestrationServiceException);
            }
        }

        private async ValueTask<AuditOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var auditOrchestrationValidationException =
                new AuditOrchestrationValidationException(
                    message: "Audit orchestration validation error occurred, please try again.",
                    innerException: exception);

            return auditOrchestrationValidationException;
        }

        private async ValueTask<AuditOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var addressOrchestrationDependencyValidationException =
                new AuditOrchestrationDependencyValidationException(
                    message: "Audit orchestration dependency validation error occurred, " +
                    "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption)!);

            return addressOrchestrationDependencyValidationException;
        }

        private async ValueTask<AuditOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var addressOrchestrationDependencyException =
                new AuditOrchestrationDependencyException(
                    message: "Audit orchestration dependency error occurred, " +
                    "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption)!);

            return addressOrchestrationDependencyException;
        }

        private async ValueTask<AuditOrchestrationServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var auditServiceException =
                new AuditOrchestrationServiceException(
                    message: "Audit orchestration service error occurred, please contact support.",
                    innerException: exception);

            return auditServiceException;
        }
    }
}
