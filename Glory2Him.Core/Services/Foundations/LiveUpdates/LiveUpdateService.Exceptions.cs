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
using Glory2Him.Core.Models.Foundations.LiveUpdates.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.LiveUpdates
{
    internal partial class LiveUpdateService
    {
        private delegate ValueTask ReturningNothingFunction();

        private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
        {
            try
            {
                await returningNothingFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLiveUpdateException =
                    new TimeoutLiveUpdateException(
                        message: "Failed live update timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogDependencyExceptionAsync(timeoutLiveUpdateException);
            }
            catch (NullLiveUpdateException nullLiveUpdateException)
            {
                throw await CreateAndLogValidationExceptionAsync(nullLiveUpdateException);
            }
            catch (InvalidLiveUpdateException invalidLiveUpdateException)
            {
                throw await CreateAndLogValidationExceptionAsync(invalidLiveUpdateException);
            }
        }

        private async ValueTask<LiveUpdateValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var liveUpdateValidationException = new LiveUpdateValidationException(
                message: "Live update validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(liveUpdateValidationException);

            return liveUpdateValidationException;
        }

        private async ValueTask<LiveUpdateDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var liveUpdateDependencyException = new LiveUpdateDependencyException(
                message: "Live update dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(liveUpdateDependencyException);

            return liveUpdateDependencyException;
        }
    }
}
