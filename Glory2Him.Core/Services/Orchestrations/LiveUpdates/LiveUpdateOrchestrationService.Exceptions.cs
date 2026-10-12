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

using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    internal partial class LiveUpdateOrchestrationService
    {
        // ONE chain for every ear, whatever entity its fact carries, so a family added for one
        // cannot be forgotten on another.
        private delegate ValueTask<T> ReturningValueFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningValueFunction<T> returningValueFunction)
        {
            try
            {
                return await returningValueFunction();
            }
            catch (InvalidLiveUpdateOrchestrationException invalidLiveUpdateOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidLiveUpdateOrchestrationException);
            }

            // the ContentItem foundation's exceptions
            catch (ContentItemValidationException contentItemValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemValidationException);
            }
            catch (ContentItemDependencyValidationException contentItemDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemDependencyValidationException);
            }
            catch (ContentItemDependencyException contentItemDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: contentItemDependencyException);
            }
            catch (ContentItemServiceException contentItemServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: contentItemServiceException);
            }
        }

        private async ValueTask<LiveUpdateOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var liveUpdateOrchestrationValidationException =
                new LiveUpdateOrchestrationValidationException(
                    message: "Live update orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: liveUpdateOrchestrationValidationException);

            return liveUpdateOrchestrationValidationException;
        }

        private async ValueTask<LiveUpdateOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var liveUpdateOrchestrationDependencyValidationException =
                new LiveUpdateOrchestrationDependencyValidationException(
                    message: "Live update orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: liveUpdateOrchestrationDependencyValidationException);

            return liveUpdateOrchestrationDependencyValidationException;
        }

        private async ValueTask<LiveUpdateOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var liveUpdateOrchestrationDependencyException =
                new LiveUpdateOrchestrationDependencyException(
                    message: "Live update orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: liveUpdateOrchestrationDependencyException);

            return liveUpdateOrchestrationDependencyException;
        }
    }
}
