// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using Glory2Him.Security.Client.Models.Foundations.Users.Exceptions;
using Xeptions;

namespace Glory2Him.Security.Client.Services.Foundations.Users
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
                throw await CreateAndLogValidationExceptionAsync(invalidArgumentUserException);
            }
            catch (ClaimNotFoundUserException claimNotFoundUserException)
            {
                throw await CreateAndLogValidationExceptionAsync(claimNotFoundUserException);
            }
            catch (Exception exception)
            {
                var failedUserServiceException =
                    new FailedUserServiceException(
                        message: "Failed user service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedUserServiceException);
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
