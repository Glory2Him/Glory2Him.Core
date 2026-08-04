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

using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Xeptions;

namespace Glory2Him.WebApp.Services.Views.Users
{
    public partial class UsersViewService
    {
        private delegate ValueTask<T> ReturningUsersFunction<T>();
        private delegate ValueTask ReturningNothingFunction();

        private async ValueTask<T> TryCatch<T>(ReturningUsersFunction<T> returningUsersFunction)
        {
            try
            {
                return await returningUsersFunction();
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                await this.loggingBroker.LogWarningAsync(usersViewValidationException.Message);

                throw;
            }
            catch (Exception exception)
            {
                var failedUsersViewServiceException =
                    new FailedUsersViewServiceException(
                        message: "Failed users view service error occurred, contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedUsersViewServiceException);
            }
        }

        private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
        {
            try
            {
                await returningNothingFunction();
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                // The message is already written for the admin — log it and let it through so the
                // page can show the real reason instead of a generic failure.
                await this.loggingBroker.LogWarningAsync(usersViewValidationException.Message);

                throw;
            }
            catch (Exception exception)
            {
                var failedUsersViewServiceException =
                    new FailedUsersViewServiceException(
                        message: "Failed users view service error occurred, contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedUsersViewServiceException);
            }
        }

        private async ValueTask<UsersViewServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var usersViewServiceException =
                new UsersViewServiceException(
                    message: "Users view service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(usersViewServiceException);

            return usersViewServiceException;
        }
    }
}
