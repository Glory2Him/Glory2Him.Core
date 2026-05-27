// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions;
using Xeptions;

namespace Glory2Him.Security.Client.Services.Foundations.Audits
{
    internal partial class AuditService
    {
        private delegate ValueTask<T> ReturningObjectFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningObjectFunction<T> returningObjectFunction)
        {
            try
            {
                return await returningObjectFunction();
            }
            catch (InvalidArgumentAuditException invalidArgumentAuditException)
            {
                throw await CreateAndLogValidationExceptionAsync(invalidArgumentAuditException);
            }
            catch (Exception exception)
            {
                var failedAuditServiceException =
                    new FailedAuditServiceException(
                        message: "Failed audit service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedAuditServiceException);
            }
        }

        private async ValueTask<AuditValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var auditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: exception);

            return auditValidationException;
        }

        private async ValueTask<AuditServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var auditServiceException =
                new AuditServiceException(
                    message: "Audit service error occurred, please contact support.",
                    innerException: exception);

            return auditServiceException;
        }
    }
}
