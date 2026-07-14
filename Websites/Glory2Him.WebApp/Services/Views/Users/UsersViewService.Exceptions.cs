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
using System.Threading.Tasks;
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
