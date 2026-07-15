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
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;
using Xeptions;

namespace Glory2Him.WebApp.Services.Views.Profiles
{
    public partial class ProfileViewService
    {
        private delegate ValueTask<T> ReturningProfileFunction<T>();
        private delegate ValueTask ReturningNothingFunction();

        private async ValueTask<T> TryCatch<T>(ReturningProfileFunction<T> returningProfileFunction)
        {
            try
            {
                return await returningProfileFunction();
            }
            catch (ProfileViewValidationException profileViewValidationException)
            {
                // Validation messages are already user-facing — log and surface as-is.
                await this.loggingBroker.LogWarningAsync(profileViewValidationException.Message);

                throw;
            }
            catch (Exception exception)
            {
                throw await CreateAndLogServiceExceptionAsync(exception);
            }
        }

        private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
        {
            try
            {
                await returningNothingFunction();
            }
            catch (ProfileViewValidationException profileViewValidationException)
            {
                await this.loggingBroker.LogWarningAsync(profileViewValidationException.Message);

                throw;
            }
            catch (Exception exception)
            {
                throw await CreateAndLogServiceExceptionAsync(exception);
            }
        }

        private async ValueTask<ProfileViewServiceException> CreateAndLogServiceExceptionAsync(
            Exception exception)
        {
            var failedProfileViewServiceException =
                new FailedProfileViewServiceException(
                    message: "Failed profile view service error occurred, contact support.",
                    innerException: exception);

            var profileViewServiceException =
                new ProfileViewServiceException(
                    message: "Profile view service error occurred, contact support.",
                    innerException: failedProfileViewServiceException);

            await this.loggingBroker.LogErrorAsync(profileViewServiceException);

            return profileViewServiceException;
        }
    }
}
