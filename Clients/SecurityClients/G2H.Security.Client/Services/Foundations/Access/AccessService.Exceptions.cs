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
using G2H.Security.Client.Models.Foundations.Access.Exceptions;
using Xeptions;

namespace G2H.Security.Client.Services.Foundations.Access
{
    internal partial class AccessService
    {
        private delegate ValueTask<T> ReturningObjectFunction<T>();

        // There is no dependency tier here, and its absence is the point: this service has no
        // broker, no store and no clock, so the only two ways it can fail are a malformed
        // request and a defect. A dependency exception would be a category with nothing in it.
        private async ValueTask<T> TryCatch<T>(ReturningObjectFunction<T> returningObjectFunction)
        {
            try
            {
                return await returningObjectFunction();
            }
            catch (InvalidArgumentAccessException invalidArgumentAccessException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidArgumentAccessException);
            }
            catch (Exception exception)
            {
                var failedAccessServiceException =
                    new FailedAccessServiceException(
                        message: "Failed access service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedAccessServiceException);
            }
        }

        private async ValueTask<AccessValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var accessValidationException =
                new AccessValidationException(
                    message: "Access validation errors occurred, please try again.",
                    innerException: exception);

            return accessValidationException;
        }

        private async ValueTask<AccessServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var accessServiceException =
                new AccessServiceException(
                    message: "Access service error occurred, please contact support.",
                    innerException: exception);

            return accessServiceException;
        }
    }
}
