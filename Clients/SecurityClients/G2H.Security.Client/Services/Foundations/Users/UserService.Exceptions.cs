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
using G2H.Security.Client.Models.Foundations.Users.Exceptions;
using Xeptions;

namespace G2H.Security.Client.Services.Foundations.Users
{
    internal partial class UserService
    {
        private delegate ValueTask<T> ReturningObjectFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningObjectFunction<T> returningObjectFunction)
        {
            try
            {
                return await returningObjectFunction();
            }
            catch (InvalidArgumentUserException invalidArgumentUserException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidArgumentUserException);
            }
            catch (ClaimNotFoundUserException claimNotFoundUserException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: claimNotFoundUserException);
            }
            catch (Exception exception)
            {
                var failedUserServiceException =
                    new FailedUserServiceException(
                        message: "Failed user service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(exception: failedUserServiceException);
            }
        }

        private async ValueTask<UserValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var userValidationException =
                new UserValidationException(
                    message: "User validation errors occurred, please try again.",
                    innerException: exception);

            return userValidationException;
        }

        private async ValueTask<UserServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var userServiceException =
                new UserServiceException(
                    message: "User service error occurred, please contact support.",
                    innerException: exception);

            return userServiceException;
        }
    }
}
